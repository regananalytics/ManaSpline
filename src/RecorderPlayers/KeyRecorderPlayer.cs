using Gma.System.MouseKeyHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace ManaSpline
{
    public class KeyRecorderPlayer : RecorderPlayerBase<KeyPressRecord>
    {
        public const string KEY = "KEY";

        private InputSimulator _inputSimulator = new InputSimulator();
        private readonly IKeyboardMouseEvents _globalHook;
        private readonly Dictionary<string, float> keyDownTimes = new Dictionary<string, float>();

        public KeyRecorderPlayer(CancellationToken token) : base(token)
        {
            _globalHook = Hook.GlobalEvents();

            if (Verbose)
                Log("[SETUP] Created KeyRecorderPlayer");
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var igt = GetIGT();
            var key = e.KeyCode.ToString();
            try
            {
                if (IsRunning && !IsPaused && !ManaSpline.memManager.IsGamePaused)
                    keyDownTimes.TryAdd(key, igt);
            }
            catch (Exception ex)
            {
                Log($"Error on KeyDown: {ex.Message}");
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            float downTime;
            var igt = GetIGT();
            var key = e.KeyCode.ToString();
            try
            {
                if (keyDownTimes.Remove(key, out downTime))
                    BuildRecord(key, downTime, igt);
            }
            catch (Exception ex)
            {
                Log($"Error on KeyUp: {ex.Message}");
            }
        }

        private void BuildRecord(string key, float igtDownTime, float igtUpTime)
        {
            // Calculate times
            float duration = (igtUpTime - igtDownTime) * 1000.0f; // ms

            if (IsRunning && !IsPaused && !ManaSpline.memManager.IsGamePaused)
            {
                var record = new KeyPressRecord(igtDownTime, key, duration);

                Task.Run(() => RecordAsync(record));
            }
        }

        protected override void DoAction(KeyPressRecord record) {}

        private async Task AsyncDoAction(KeyPressRecord record)
        {
            // Simulate key press and release with InputSimulator
            if (Enum.TryParse($"VK_{record.Key.ToUpper()}", out VirtualKeyCode keyCode))
            {
                _inputSimulator.Keyboard.KeyDown(keyCode);
                await Task.Delay((int)record.Duration, _token);
                _inputSimulator.Keyboard.KeyUp(keyCode);
            }
        }

        protected override async Task SimulateAction(KeyPressRecord record, uint delay)
        {
            try
            {
                if (delay > 0)
                        await Task.Delay((int)delay, _token);

                await AsyncDoAction(record);

                if (Verbose)
                {
                    var delta = (GetIGT() - record.IGT) * 1000.0f;
                    Log($"{record}, ΔIGT: {delta} ms");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in SimulateAction: {ex.Message}");
            }
        }
        
        public override string SerializeRecord(KeyPressRecord record)
        {
            return JsonConvert.SerializeObject(new
            {
                IGT = record.IGT,
                KEY = new
                {
                    IGT = record.IGT,
                    Key = record.Key,
                    Duration = record.Duration
                }
            });
        }

        public override KeyPressRecord DeserializeRecord(dynamic key)
        {
            return new KeyPressRecord
            (
                (float)key.IGT,
                (string)key.Key,
                (float)key.Duration
            );
        }

        public override void Start()
        {
            base.Start();
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.KeyUp += OnKeyUp;
        }

        public override void Stop()
        {
            base.Stop();
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.KeyUp -= OnKeyUp;
        }
    }

    public class KeyPressRecord : Record
    {
        public string Key {get; set; }
        public float Duration { get; set; }

        public KeyPressRecord(float igt, string key, float duration) : base(igt)
        {
            Key = key;
            Duration = duration;
        }
    }
}