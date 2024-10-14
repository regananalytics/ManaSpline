using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public interface IRecorderPlayer
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        void Start();
        void Play();
        void Pause();
        void Resume();
        void Stop();
    }

    public abstract class RecorderPlayerBase<TRecord>: IRecorderPlayer where TRecord : Record
    {
        protected CancellationToken _token;
        protected readonly BlockingCollection<TRecord> _playbackQueue = new BlockingCollection<TRecord>();

        protected bool Batch = false;
        protected float batchWindow = 0.5f;

        protected MemManager memManager => ManaSpline.memManager;
        protected RecordingFileWriter _fileWriter => ManaSpline.FileWriter;

        public bool IsRunning { get; private set;} = false;
        public bool IsPaused { get; private set;} = false;

        protected void Log(string log) => ManaSpline.Log(log);

        public bool Verbose => ManaSpline.Verbose;

        public RecorderPlayerBase(CancellationToken token)
        {
            _token = token;
        }

        public virtual void Start()
        {
            IsRunning = true;
            IsPaused = false;
        }

        public virtual void Play()
        {
            IsRunning = true;
            IsPaused = false;
            Task.Run(() => ExecutePlayback());
        }

        public virtual void Stop()
        {
            IsRunning = false;
            IsPaused = false;
            _playbackQueue.CompleteAdding();
        }

        public virtual void Pause()
        {
            if (IsRunning)
                IsPaused = true;
        }

        public virtual void Resume()
        {
            if (IsRunning && IsPaused)
                IsPaused = false;
        }

        protected virtual async Task ExecutePlayback()
        {
            while (!_token.IsCancellationRequested)
            {
                // Wait until we get queued records
                if (_playbackQueue.Count == 0)
                {
                    await Task.Delay(100, _token);
                    continue;
                }

                if (Batch)
                {
                    var batches = new List<(float firstIGT, List<(TimeSpan delay, TRecord record)>)>();
                    var batch = new List<(TimeSpan delay, TRecord record)>();
                    float firstIGT = 0;

                    // build batches
                    foreach (var record in _playbackQueue.GetConsumingEnumerable(_token))
                    {
                        if (batch.Count == 0)
                        {
                            firstIGT = record.IGT;
                            batch.Add((TimeSpan.Zero, record));
                            continue;
                        }

                        float nextIGT = record.IGT;
                        float delay = nextIGT - firstIGT;
                        if (delay <= batchWindow)
                        {
                            batch.Add((TimeSpan.FromMilliseconds(delay), record));
                            continue;
                        }
                        
                        batches.Add((firstIGT, batch));
                        batch = new List<(TimeSpan delay, TRecord record)>();
                    }

                    // Execute Batches
                    foreach (var batchTuple in batches)
                    {
                        var (_firstIGT, _batch) = batchTuple;

                        uint delay = (uint)Math.Max(Math.Round((_firstIGT - GetIGT()) * 1000.0f), 0);
                        if (delay > 1000)
                            delay = await SmartDelayAsync(firstIGT, minDelay: 100);
                        
                        if (_batch.Count > 1)
                        {
                            await SimulateBatch(_batch, delay);
                        }
                        else
                        {
                            var (_, _record) = _batch[0];
                            await SimulateAction(_record, delay);
                        }
                            
                    }
                }
                else
                {
                    foreach (var record in _playbackQueue.GetConsumingEnumerable(_token))
                    {
                        float igt = GetIGT();
                        uint delay = (uint)Math.Max((record.IGT - igt) * 1000.0f, 0);

                        if (delay > 1000)
                            delay = await SmartDelayAsync(record.IGT, minDelay: 100);
                            
                        await SimulateAction(record, delay);
                    }
                }
            }
        }

        protected virtual async Task SimulateAction(TRecord record, uint delay)
        {
            if (delay > 0)
                await Task.Delay((int)delay, _token);

            DoAction(record);

            if (Verbose)
            {
                var delta = (GetIGT() - record.IGT) * 1000.0f;
                Log($"{record}, ΔIGT: {delta} ms");
            }
        }

        protected virtual async Task SimulateBatch(List<(TimeSpan delay, TRecord record)> batch, uint initialDelay)
        {
            bool firstRecord = true;

            foreach (var recordTuple in batch)
            {
                var (delay, record) = recordTuple;

                if (firstRecord)
                {
                    firstRecord = false;
                    await Task.Delay((int)initialDelay, _token);
                }
                else
                {
                    await Task.Delay(delay, _token);
                }

                DoAction(record);
            }
        }

        public virtual async Task RecordAsync(TRecord record)
        {
            if (IsRunning && !IsPaused && !memManager.IsGamePaused)
            {
                var json = SerializeRecord(record);
                await _fileWriter.EnqueueRecord(json);
            }
        }

        public void QueuePlay(TRecord record)
        {
            if (!_playbackQueue.IsAddingCompleted)
                _playbackQueue.Add(record, _token);
        }

        public async ValueTask<uint> SmartDelayAsync(float target, float minDelay = 1)
        {
            const float decayFactor = 0.5f;
            float remainingDelay = 0f;

            while (true)
            {
                // Get the current IGT at the start of each loop
                float currentIGT = GetIGT();
                remainingDelay = (target - currentIGT) * 1000.0f;

                if (remainingDelay < minDelay)
                {
                    break;
                }
                else if (remainingDelay < 100)
                {
                    await Task.Delay((int)remainingDelay, _token);
                    break;
                }
                else
                {
                    // Calculate the next delay segment using the decay factor
                    float delaySegment = remainingDelay * decayFactor;

                    Log($"Smart Delay: {remainingDelay / 1000.0f} sec, waiting {delaySegment} ms");

                    if (IsGamePaused)
                        await ManaSpline.memManager.WaitForUnpasuedAsync(_token);

                    await Task.Delay((int)delaySegment, _token);
                }
            }
            return (uint)Math.Max(remainingDelay - 1, 0);
        }

        // Abstract methods that must be implemented
        protected abstract void DoAction(TRecord record);
        public abstract string SerializeRecord(TRecord record);
        public abstract TRecord DeserializeRecord(dynamic json);

        // Time Helpers
        public float GetIGT() => ManaSpline.GetIGT();
        public bool IsGamePaused => ManaSpline.IsGamePaused;

        public virtual void Dispose()
        {
            Stop();
            _playbackQueue.Dispose();
        }
    }

    public abstract class Record
    {
        public float IGT { get; set; }

        public Record(float igt)
        {
            IGT = igt;
        }
    }
}