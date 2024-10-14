using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public class PlaybackFileReader : IDisposable
    {
        private readonly string _filePath;
        private StreamReader _reader;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _playbackTask;
        private bool _disposed = false;

        // RecordPlayers
        public static KeyRecorderPlayer keyRecorderPlayer;
        public static MouseRecorderPlayer mouseRecorderPlayer;
        public static StateRecorderPlayer stateRecorderPlayer;

        // Next record to be processes
        private Record _nextRecord;

        public float GetIGT() => ManaSpline.GetIGT();

        public PlaybackFileReader(string filePath)
        {
            _filePath = filePath;
            ManaSpline.Log($"Loading playback file: {_filePath}");

            // Get RecorderPlayers
            keyRecorderPlayer = ManaSpline.keyRecorderPlayer;
            mouseRecorderPlayer = ManaSpline.mouseRecorderPlayer;
            stateRecorderPlayer = ManaSpline.stateRecorderPlayer;
        }

        public void Start()
        {
            // Start playback
            _playbackTask = Task.Run(() => ProcessPlaybackAsync(_cts.Token), _cts.Token);
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task ProcessPlaybackAsync(CancellationToken token)
        {
            ManaSpline.Log("Starting Playback...");

            try
            {
                using (_reader = new StreamReader(_filePath))
                {
                    while (!_reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        // Read next recordx if we don't ahve one
                        if (_nextRecord == null)
                        {
                            string line = await _reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            try
                            {
                                _nextRecord = JsonConvert.DeserializeObject<Record>(line);
                            }
                            catch (Exception ex)
                            {
                                ManaSpline.Log($"Error parsing line: {ex.Message}");
                                continue;
                            }
                        }

                        // Get current IGT
                        float currentIGT = GetIGT();
                        var nextIGT = _nextRecord.IGT;

                        if (currentIGT > nextIGT)
                            continue;

                        DispatchRecord(_nextRecord);
                        _nextRecord = null;

                    }
                }
            }
            catch (OperationCanceledException)
            {
                ManaSpline.Log("Playback canceled.");
            }
            catch (Exception ex)
            {
                ManaSpline.Log($"Error during playback: {ex.Message}");
            }
        }

        private void DispatchRecord(Record record)
        {
            try
            {
                // Dispatch to the appropriate player based on the record context
                if (record.KEY != null)
                {
                    var keyPressRecord = keyRecorderPlayer.DeserializeRecord(record.KEY);
                    keyRecorderPlayer.QueuePlay(keyPressRecord);
                }

                if (record.MOUSE != null)
                {
                    var mousePoint = mouseRecorderPlayer.DeserializeRecord(record.MOUSE);
                    mouseRecorderPlayer.QueuePlay(mousePoint);
                }

                if (record.STATE != null)
                {
                    var stateRecord = stateRecorderPlayer.DeserializeRecord(record.STATE);
                    stateRecorderPlayer.QueuePlay(stateRecord);
                }
            }
            catch (Exception ex)
            {
                ManaSpline.Log($"Error dispatching record: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cts.Cancel();
                _playbackTask?.Wait();
                _cts.Dispose();
                _reader?.Dispose();
            }
        }

        public class Record
        {
            public float IGT { get; set; }
            public dynamic KEY { get; set; }
            public dynamic MOUSE { get; set; }
            public dynamic STATE { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> AdditionalData { get; set; }
        }
    }
}