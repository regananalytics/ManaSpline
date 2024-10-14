using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ManaSpline
{
    public partial class ManaForm : Form
    {
        // Modifier Keys
        [Flags]
        public enum Modifiers : uint
        {
            None = 0,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            Win = 0x0008,
        }

        // PInvoke Singatures for RegisterHotKey and UnregisterHotKey
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, Modifiers fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Unique IDs for each hotkey
        private const int HOTKEY_ID_RECORD = 9000;
        private const int HOTKEY_ID_PLAY = 9001;
        private const int HOTKEY_ID_PAUSE = 9002;
        private const int HOTKEY_ID_STOP = 9003;

        // Media key vk codes
        private const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        private const uint VK_MEDIA_PREV_TRACK = 0xB1;
        private const uint VK_MEDIA_STOP = 0xB2;
        private const uint VK_MEDIA_PLAY_PAUSE = 0xB3;

        private void RegisterGlobalHotKeys()
        {
            // Register Play
            bool playRegistered = RegisterHotKey(
                this.Handle,
                HOTKEY_ID_PLAY,
                Modifiers.None,
                VK_MEDIA_PLAY_PAUSE
            );
            if (!playRegistered)
                MessageBox.Show("Failed to register Play hotkey.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Register Record
            bool recordRegistered = RegisterHotKey(
                this.Handle,
                HOTKEY_ID_RECORD,
                Modifiers.None,
                VK_MEDIA_NEXT_TRACK
            );
            if (!recordRegistered)
                MessageBox.Show("Failed to register Record hotkey.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Register Pause
            bool pauseRegistered = RegisterHotKey(
                this.Handle,
                HOTKEY_ID_PAUSE,
                Modifiers.None,
                VK_MEDIA_PREV_TRACK
            );
            if (!pauseRegistered)
                MessageBox.Show("Failed to register Pause hotkey.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Register Stop
            bool stopRegistered = RegisterHotKey(
                this.Handle,
                HOTKEY_ID_STOP,
                Modifiers.None,
                VK_MEDIA_STOP
            );
            if (!stopRegistered)
                MessageBox.Show("Failed to register Stop hotkey.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DispatchGlobalHotKey(int id)
        {
            switch(id)
            {
                case HOTKEY_ID_RECORD:
                    BtnRecord_Click(this, EventArgs.Empty);
                    break;
                case HOTKEY_ID_PLAY:
                    BtnPlay_Click(this, EventArgs.Empty);
                    break;
                case HOTKEY_ID_PAUSE:
                    BtnPlay_Click(this, EventArgs.Empty);
                    break;
                case HOTKEY_ID_STOP:
                    BtnStop_Click(this, EventArgs.Empty);
                    break;
            }
        }

        private void UnregisterGlobalHotKeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_ID_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE);
            UnregisterHotKey(this.Handle, HOTKEY_ID_STOP);
        }
    }
}