using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using System;
using System.Windows.Forms;


namespace ManaSpline
{
    public partial class ManaForm : Form
    {
        public event EventHandler<RawInputEventArgs> Input;

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_INPUT = 0x00FF;

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                DispatchGlobalHotKey(id);
            }
            else if (m.Msg == WM_INPUT)
            {
                var data = RawInputData.FromHandle(m.LParam);
                Input?.Invoke(this, new RawInputEventArgs(data));
            }
            base.WndProc(ref m);
        }
    }

    public class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(RawInputData data)
        {
            Data = data;

            switch (data)
            {
                case RawInputMouseData mouse:
                    Mouse = mouse.Mouse; 
                    break;
                case RawInputKeyboardData keyboard:
                    Keyboard = keyboard.Keyboard;
                    break;
                case RawInputHidData hid:
                    Hid = hid.Hid;
                    break;
            }
        }

        public RawInputData Data { get; }
        public RawMouse Mouse { get; }
        public RawKeyboard Keyboard { get; }
        public RawHid Hid { get; }
    }
}