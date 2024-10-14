using Newtonsoft.Json;
using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace ManaSpline
{
    public class MouseRecorderPlayer : RecorderPlayerBase<MousePoint>
    {
        public const string KEY = "MOUSE";
        private InputSimulator _inputSimulator = new InputSimulator();
        private float _lastRecordTime = 0;

        public MouseRecorderPlayer(CancellationToken token) : base(token)
        {
            if (Verbose)
                Log($"[SETUP] MouseRecorderPlayer created.");
        }

        private void RegisterRawInput()
        {
            RawInputDevice.RegisterDevice(
                HidUsageAndPage.Mouse,
                RawInputDeviceFlags.InputSink | RawInputDeviceFlags.NoLegacy, 
                ManaSpline.window.Handle
            );
        }

        private void DeRegisterRawInput()
        {
            RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
        }

        private void OnRawMouseInput(object sender, RawInputEventArgs e)
        {
            var igt = GetIGT();
            var deltaTime = (igt - _lastRecordTime) * 1000.0f;

            // Get absolute position
            var cursorPos = Cursor.Position;
            int absX = cursorPos.X;
            int absY = cursorPos.Y;

            var record = new MousePoint(igt, absX, absY, e.Mouse.LastX, e.Mouse.LastY);

            _lastRecordTime = igt;

            Task.Run(() => RecordAsync(record));
        }

        protected override void DoAction(MousePoint record)
        {
            _inputSimulator.Mouse.MoveMouseBy(record.DeltaX, record.DeltaY);
        }

        public override string SerializeRecord(MousePoint record)
        { 
            return JsonConvert.SerializeObject(new
            {
                IGT = record.IGT,
                MOUSE = new
                {
                    IGT = record.IGT,
                    MouseX = record.MouseX,
                    MouseY = record.MouseY,
                    DeltaX = record.DeltaX,
                    DeltaY = record.DeltaY
                }
            });
        }

        public override MousePoint DeserializeRecord(dynamic mouse)
        {
            return new MousePoint
            (
                (float)mouse.IGT,
                (int)mouse.MouseX,
                (int)mouse.MouseY,
                (int)mouse.DeltaX,
                (int)mouse.DeltaY
            );
        }

        public override void Start()
        {
            base.Start();
            RegisterRawInput();
            ManaSpline.window.Input += OnRawMouseInput;
        }

        public override void Stop()
        {
            base.Stop();
            ManaSpline.window.Input += OnRawMouseInput;
            DeRegisterRawInput();
        }
    }

    public class MousePoint : Record
    {
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public int DeltaX { get; set; }
        public int DeltaY { get; set; }

        public MousePoint(float igt, int x, int y, int dx, int dy) : base(igt)
        {
            MouseX = x;
            MouseY = y;
            DeltaX = dx;
            DeltaY = dy;
        }
    }
}