using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ManaSpline
{
    public static class ManaSpline
    {
        // Window Handle
        public static ManaForm window { get; set; }

        // Config
        public static Dictionary<string, object> config { get; set;}

        // MemManager
        public static MemManager memManager { get; set; }
        public static RecordingPlaybackManager recordingPlaybackManager { get; set; }
        public static KeyRecorderPlayer keyRecorderPlayer { get; set; }
        public static MouseRecorderPlayer mouseRecorderPlayer { get; set; }
        public static StateRecorderPlayer stateRecorderPlayer { get; set; }
        public static RecordingFileWriter FileWriter { get; set; }
        public static PlaybackFileReader FileReader { get; set; }

        public static Label outputTime { get; set; }
        public static TextBox outputLog { get; set; }

        // States
        public static bool Verbose => (bool)config["Verbose"];

        // Delegate for logging messages
        public delegate void LogHandler(string message);
        public static LogHandler Log { get; set; }

        // Time Getter
        public static bool IsGamePaused => memManager?.IsGamePaused ?? true;
        public static float GetIGT() => memManager?.GetIGT() ?? 0f;
        public static (float igt, float delta) GetIGTDelta() => memManager?.GetIGTDelta() ?? (0f, 0f);
    }

    public partial class ManaForm : Form
    {
        private RecordingPlaybackManager _recordingPlaybackManager;
        private ConfigurationManager _configManager;

        private int _igtUpdateInterval = 100;

        public ManaForm()
        {
            InitializeComponent();
            RegisterGlobalHotKeys();

            // Load Configuration
            _configManager = new ConfigurationManager();

            // ManaSpline Attributes
            ManaSpline.window = this;
            ManaSpline.Log = AppendLog;

            // Load Configuration into UI
            LoadControlStates();

            // Event Handlers
            RegisterControlEvents();

            // Save Configuration
            SaveControlStates();
        }

        private void InitRecordingPlaybackManager()
        {
            if (
                string.IsNullOrWhiteSpace(ManaSpline.config["OutputDir"] as string) 
                || string.IsNullOrWhiteSpace(ManaSpline.config["OutputFile"] as string)
            )
            {
                MessageBox.Show(
                    "Please provide valid paths for configuration and output.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            if (_recordingPlaybackManager == null)
            {
                ManaSpline.Log("ManaForm - Init RecordingPlayback Manager");
                _recordingPlaybackManager = new RecordingPlaybackManager();
            }
            
            Task.Run(() => UpdateIgtText());

            bool recordingEnabled = (
                (bool)ManaSpline.config["EnableKeyRecording"] 
                || (bool)ManaSpline.config["EnableMouseRecording"] 
                || (bool)ManaSpline.config["EnableStateRecording"]
            );
            if (recordingEnabled)
            {
                ManaSpline.Log("ManaForm - Init File Writer");
                ManaSpline.FileWriter = new RecordingFileWriter(
                    Path.Combine((string)ManaSpline.config["OutputDir"], 
                    (string)ManaSpline.config["OutputFile"])
                );
            }
                
            bool playbackEnabled = (
                (bool)ManaSpline.config["EnableKeyPlayback"] 
                || (bool)ManaSpline.config["EnableMousePlayback"] 
                || (bool)ManaSpline.config["EnableStatePlayback"]
            );
            if (playbackEnabled)
            {
                ManaSpline.Log("ManaForm - Init File Reader");
                ManaSpline.FileReader = new PlaybackFileReader(
                    (string)ManaSpline.config["PlaybackFile"]
                );
            }
        }

        private async Task UpdateIgtText()
        {
            while (true)
            {
                try
                {
                    (float igt, float delta) = ManaSpline.GetIGTDelta();

                    if (!ManaSpline.IsGamePaused)
                        igt += delta;

                    TimeSpan timeSpan = TimeSpan.FromSeconds((double) igt);
                    string formattedIGT = timeSpan.ToString(@"mm\:ss\.ff");

                    string text = $"IGT {formattedIGT}";

                    var outputTime = ManaSpline.outputTime;
                    if (outputTime.InvokeRequired)
                        outputTime.Invoke(new Action(() => outputTime.Text = text));
                    else
                        outputTime.Text = text;

                    await Task.Delay(_igtUpdateInterval);
                }
                catch (Exception) {}
            }
        }

        private void AppendLog(string message)
        {
            var outputLog = ManaSpline.outputLog;
            if (outputLog.InvokeRequired)
                outputLog.Invoke(new Action(() => AppendLogMessage(message)));
            else
                AppendLogMessage(message);
        }

        private void AppendLogMessage(string message)
        {
            var outputLog = ManaSpline.outputLog;
            string msg = message;
            if (!string.IsNullOrWhiteSpace(outputLog.Text))
                msg = $"{Environment.NewLine}{message}";
            outputLog.AppendText(msg);
            outputLog.SelectionStart = outputLog.Text.Length;
            outputLog.ScrollToCaret();
            Console.WriteLine(msg);
        }

        private void StartRecording()
        {
            InitRecordingPlaybackManager();
            _recordingPlaybackManager.StartRecording();
        }

        private void StartPlayback()
        {
            InitRecordingPlaybackManager();
            _recordingPlaybackManager.StartPlayback();
        }

        private void StopRecording()
        {
            if (_recordingPlaybackManager != null)
            {
                _recordingPlaybackManager.Stop();
                _recordingPlaybackManager = null;
            }

            var outputFilePath = Path.Combine(
                (string)ManaSpline.config["OutputDir"], (string)ManaSpline.config["OutputFile"]
            );
            var postProcessor = new RecordPostProcessor(outputFilePath);
            postProcessor.Process();
        } 

        private void StopPlayback()
        {
            if (_recordingPlaybackManager != null)
            {
                _recordingPlaybackManager.Stop();
                _recordingPlaybackManager = null;
            }
        }
    }
}