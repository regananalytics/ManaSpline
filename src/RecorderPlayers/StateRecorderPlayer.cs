using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public class StateRecorderPlayer : RecorderPlayerBase<StateRecord>
    {
        public const string KEY = "STATE";
        private readonly MemManager _memManager;
        private readonly int _recordIntervalMs;

        new protected bool Batch = true;

        public StateRecorderPlayer(CancellationToken token) : base(token)
        {
            _memManager = ManaSpline.memManager;
            _recordIntervalMs = Convert.ToInt32(ManaSpline.config["StateInterval"]);

            if (Verbose)
               Log("[SETUP] StateRecordPlayer created.");
        }

        private async Task StartRecordingAsync()
        {
            while (!_token.IsCancellationRequested)
            {
                if (IsRunning && !IsPaused && !_memManager.IsGamePaused)
                {
                    var state = _memManager.LastGameState;
                    if (state != null)
                    {
                        var currentIGT = _memManager.GetGameIGT(state);
                        var record = new StateRecord(currentIGT, (float)state["X"], (float)state["Y"], (float)state["Z"]);

                        await RecordAsync(record);
                    }
                }
                await Task.Delay(_recordIntervalMs, _token);
            }
        }

        protected override void DoAction(StateRecord record)
        {
            ManaSpline.memManager.MemCore.ProcessPointers["X"].SetValue(record.X);
            ManaSpline.memManager.MemCore.ProcessPointers["Y"].SetValue(record.Y);
            ManaSpline.memManager.MemCore.ProcessPointers["Z"].SetValue(record.Z);
        }


        public override string SerializeRecord(StateRecord record)
        {
            return JsonConvert.SerializeObject(new
            {
                IGT = record.IGT,
                STATE = new
                {
                    IGT = record.IGT,
                    X = record.X,
                    Y = record.Y,
                    Z = record.Z,
                }
            });
        }

        public override StateRecord DeserializeRecord(dynamic json)
        {
            // var gameState = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return new StateRecord(
                (float)json.IGT,
                (float)json.X, 
                (float)json.Y, 
                (float)json.Z
            );
        }

        public override void Start()
        {
            base.Start();
            Task.Run(() => StartRecordingAsync(), _token);
        }

        public override void Play()
        {
            base.Play();
        }
    }

    public class StateRecord : Record
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public StateRecord(float igt, float x, float y, float z) : base(igt)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

}