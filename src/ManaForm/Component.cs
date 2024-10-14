using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ManaSpline
{
    public partial class ManaForm
    {
        private const string ConfigFilePath = "config.json";

        public bool IsRecording => _recordingPlaybackManager?.IsRecording ?? false;
        public bool IsPlaying => _recordingPlaybackManager?.IsPlaying ?? false;
        public bool IsPaused => _recordingPlaybackManager?.IsPaused ?? false;

        private Button btnRecord;
        private Button btnPlay;
        private Button btnPause;
        private Button btnStop;

        private TextBox txtGameConfig;
        private TextBox txtOutputDir;
        private TextBox txtPlaybackFile;

        public List<Control> IgnoreConfig = new List<Control>();

        // Main Component
        private void InitializeComponent()
        {
            // Initialize components
            int _width = 800;
            int _height = 880;
            int _lpad = 20;

            int _media_height = _height - 130;

            this.Text = "ManaSpline - Game Recording Configuration";
            this.Size = new Size(_width, _height);

            // Game Name
            Label lblGameConfig = new Label();
            lblGameConfig.Text = "Game Config Path:";
            lblGameConfig.Location = new Point(_lpad, 20);
            lblGameConfig.AutoSize = true;

            txtGameConfig = new TextBox();
            txtGameConfig.Name = "GameConfig";
            txtGameConfig.Text = "";
            txtGameConfig.Location = new Point(180, 20);
            txtGameConfig.Size = new Size(470, 20);

            Button btnGameConfigBrowse = new Button();
            btnGameConfigBrowse.Text = "...";
            btnGameConfigBrowse.Location = new Point(660, 20);
            btnGameConfigBrowse.AutoSize = true;
            btnGameConfigBrowse.Click += new EventHandler(BrowseGameConfigPath);

            // Output File Path
            Label lblOutputDir = new Label();
            lblOutputDir.Text = "Output Directory:";
            lblOutputDir.Location = new Point(_lpad, 60);
            lblOutputDir.AutoSize = true;

            txtOutputDir = new TextBox();
            txtOutputDir.Name = "OutputDir";
            txtOutputDir.Location = new Point(180, 60);
            txtOutputDir.Size = new Size(470, 20);

            Button btnOutputDirBrowse = new Button();
            btnOutputDirBrowse.Text = "...";
            btnOutputDirBrowse.Location = new Point(660, 60);
            btnOutputDirBrowse.AutoSize = true;
            btnOutputDirBrowse.Click += new EventHandler(BrowseOutputDirPath);

            // Panel for Output File Name
            Label lblOutputFile = new Label();
            lblOutputFile.Text = "Output File Name:";
            lblOutputFile.Location = new Point(_lpad, 100);
            lblOutputFile.AutoSize = true;

            TextBox txtOutputFile = new TextBox();
            txtOutputFile.Name = "OutputFile";
            txtOutputFile.Location = new Point(180, 100);
            txtOutputFile.Size = new Size(470, 20);

            // Input Playback File
            Label lblPlaybackFile = new Label();
            lblPlaybackFile.Text = "Playback File:";
            lblPlaybackFile.Location = new Point(_lpad, 140);
            lblPlaybackFile.AutoSize = true;

            txtPlaybackFile = new TextBox();
            txtPlaybackFile.Name = "PlaybackFile";
            txtPlaybackFile.Location = new Point(180, 140);
            txtPlaybackFile.Size = new Size(470, 20);

            Button btnPlaybackFileBrowse = new Button();
            btnPlaybackFileBrowse.Text = "...";
            btnPlaybackFileBrowse.Location = new Point(660, 140);
            btnPlaybackFileBrowse.AutoSize = true;
            btnPlaybackFileBrowse.Click += new EventHandler(BrowsePlaybackFilePath);

            // Toggles for Recording
            Label lblRecording = new Label();
            lblRecording.Text = "Recording Options:";
            lblRecording.Location = new Point(60, 200);
            lblRecording.AutoSize = true;

            CheckBox chkKeyRecording = new CheckBox();
            chkKeyRecording.Name = "EnableKeyRecording";
            chkKeyRecording.Text = "Enable Key Recording";
            chkKeyRecording.Location = new Point(80, 230);
            chkKeyRecording.AutoSize = true;

            CheckBox chkMouseRecording = new CheckBox();
            chkMouseRecording.Name = "EnableMouseRecording";
            chkMouseRecording.Text = "Enable Mouse Recording";
            chkMouseRecording.Location = new Point(80, 260);
            chkMouseRecording.AutoSize = true;

            CheckBox chkStateRecording = new CheckBox();
            chkStateRecording.Name = "EnableStateRecording";
            chkStateRecording.Text = "Enable State Recording";
            chkStateRecording.Location = new Point(80, 290);
            chkStateRecording.AutoSize = true;

            // Recording Intervals
            NumericUpDown mouseInterval = new NumericUpDown();
            mouseInterval.Name = "MouseInterval";
            mouseInterval.Location = new Point(320, 260);
            mouseInterval.Size = new Size(80, 20);
            mouseInterval.Minimum = 10;
            mouseInterval.Maximum = 100;
            mouseInterval.Value = 15;

            NumericUpDown stateInterval = new NumericUpDown();
            stateInterval.Name = "StateInterval";
            stateInterval.Location = new Point(320, 290);
            stateInterval.Size = new Size(80, 20);
            stateInterval.Minimum = 10;
            stateInterval.Maximum = 5000;
            stateInterval.Value = 500;

            // Toggles for Playback (Key, Mouse, State)
            Label lblPlayback = new Label();
            lblPlayback.Text = "Playback Options:";
            lblPlayback.Location = new Point(440, 200);
            lblPlayback.AutoSize = true;

            CheckBox chkKeyPlayback = new CheckBox();
            chkKeyPlayback.Name = "EnableKeyPlayback";
            chkKeyPlayback.Text = "Enable Key Playback";
            chkKeyPlayback.Location = new Point(460, 230);
            chkKeyPlayback.AutoSize = true;

            CheckBox chkMousePlayback = new CheckBox();
            chkMousePlayback.Name = "EnableMousePlayback";
            chkMousePlayback.Text = "Enable Mouse Playback";
            chkMousePlayback.Location = new Point(460, 260);
            chkMousePlayback.AutoSize = true;

            CheckBox chkStatePlayback = new CheckBox();
            chkStatePlayback.Name = "EnableStatePlayback";
            chkStatePlayback.Text = "Enable State Playback";
            chkStatePlayback.Location = new Point(460, 290);
            chkStatePlayback.AutoSize = true;

            // Verbose Output
            CheckBox chkVerbose = new CheckBox();
            chkVerbose.Name = "Verbose";
            chkVerbose.Text = "Verbose";
            chkVerbose.Location = new Point(_lpad, _height - 100);
            chkVerbose.AutoSize = true;

            // Record Button
            var buttonFont = new Font("Arial", 24);
            
            btnRecord = new Button();
            btnRecord.Name = "btnRecord";
            btnRecord.Text = "\u23FA";
            btnRecord.Font = buttonFont;
            btnRecord.Size = new Size(64, 64);
            btnRecord.Location = new Point(_width - 340, _media_height);
            btnRecord.BackColor = Color.White;
            btnRecord.ForeColor = Color.Red;
            btnRecord.Click += BtnRecord_Click;

            // Play Recording Button
            btnPlay = new Button();
            btnPlay.Name = "btnPlay";
            btnPlay.Text = "\u23F5";
            btnPlay.Font = buttonFont;
            btnPlay.Size = new Size(64, 64);
            btnPlay.Location = new Point(_width - 260, _media_height);
            btnPlay.BackColor = Color.White;
            btnPlay.ForeColor = Color.Black;
            btnPlay.Click += BtnPlay_Click;

            // Pause Recording Button
            btnPause = new Button();
            btnPause.Name = "btnPause";
            btnPause.Text = "\u23F8";
            btnPause.Font = buttonFont;
            btnPause.Size = new Size(64, 64);
            btnPause.Location = new Point(_width - 180, _media_height);
            btnPause.BackColor = Color.White;
            btnPause.ForeColor = Color.Black;
            btnPause.Click += BtnPause_Click;

            // Stop Recording Button
            btnStop = new Button();
            btnStop.Name = "btnStop";
            btnStop.Text = "\u23F9";
            btnStop.Font = buttonFont;
            btnStop.Size = new Size(64, 64);
            btnStop.Location = new Point(_width - 100, _media_height);
            btnStop.BackColor = Color.White;
            btnStop.ForeColor = Color.Black;
            btnStop.Click += BtnStop_Click;

            // Textbox for output
            Label lblLogOutput = new Label();
            lblLogOutput.Text = "Log Output:";
            lblLogOutput.Location = new Point(_lpad, 390); 
            lblLogOutput.AutoSize = true;

            Label lblIGT = new Label();
            lblIGT.Text = "IGT 00:00.00";
            lblIGT.TextAlign = ContentAlignment.MiddleRight;
            lblIGT.Location = new Point(_width - _lpad - 160, 390); 
            lblIGT.Size = new Size(160, 20);
            lblIGT.AutoSize = true;
            ManaSpline.outputTime = lblIGT;

            TextBox txtLogOutput = new TextBox();
            txtLogOutput.Name = "txtLogOutput";
            txtLogOutput.Location = new Point(_lpad, 420); 
            txtLogOutput.Size = new Size(_width - 60, 300);
            txtLogOutput.Multiline = true;
            txtLogOutput.ScrollBars = ScrollBars.Vertical;
            txtLogOutput.ReadOnly = true;
            txtLogOutput.BackColor = Color.White;
            IgnoreConfig.Add(txtLogOutput);
            ManaSpline.outputLog = txtLogOutput;

            // Add Controls to the Form
            this.Controls.Add(lblGameConfig);
            this.Controls.Add(txtGameConfig);
            this.Controls.Add(btnGameConfigBrowse);
            this.Controls.Add(lblOutputDir);
            this.Controls.Add(txtOutputDir);
            this.Controls.Add(btnOutputDirBrowse);
            this.Controls.Add(lblOutputFile);
            this.Controls.Add(txtOutputFile);
            this.Controls.Add(lblPlaybackFile);
            this.Controls.Add(txtPlaybackFile);
            this.Controls.Add(btnPlaybackFileBrowse);
            this.Controls.Add(lblRecording);
            this.Controls.Add(chkKeyRecording);
            this.Controls.Add(chkMouseRecording);
            this.Controls.Add(chkStateRecording);
            this.Controls.Add(mouseInterval);
            this.Controls.Add(stateInterval);
            this.Controls.Add(lblPlayback);
            this.Controls.Add(chkKeyPlayback);
            this.Controls.Add(chkMousePlayback);
            this.Controls.Add(chkStatePlayback);
            this.Controls.Add(lblLogOutput);
            this.Controls.Add(lblIGT);
            this.Controls.Add(txtLogOutput);
            this.Controls.Add(chkVerbose);
            this.Controls.Add(btnRecord);
            this.Controls.Add(btnPlay);
            this.Controls.Add(btnPause);
            this.Controls.Add(btnStop);
        }

        // Callbacks
        private void BtnRecord_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsRecording)
                {
                    // Start Recording
                    StartRecording();
                    SetRecordingButtonState(isRecording: true);
                }
                else
                {
                    // Stop Recording
                    StopRecording();
                    SetRecordingButtonState(isRecording: false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error during recording: {ex.Message}");
            }
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsPlaying)
                {
                    StartPlayback();
                    SetPlaybackButtonState(isPlaying: true);
                }
                else
                {
                    StopPlayback();
                    SetPlaybackButtonState(isPlaying: false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error during playback: {ex.Message}");
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsPaused)
                {
                    _recordingPlaybackManager?.Pause();
                    SetPauseButtonState(isPaused: true);
                }
                else
                {
                    _recordingPlaybackManager?.Resume();
                    SetPauseButtonState(isPaused: false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error during pause/resume: {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsRecording || IsPlaying)
                {
                    _recordingPlaybackManager?.Stop();
                    ResetButtonStates();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error during stop: {ex.Message}");
            }
        }

        private void BrowseFilePath(TextBox targetTextBox, string filter, bool isFolderPicker = false)
        {
            if (isFolderPicker)
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Select Folder";
                    dlg.SelectedPath = targetTextBox.Text;
                    dlg.ShowNewFolderButton = true;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        targetTextBox.Text = dlg.SelectedPath;
                    }
                }
            }
            else
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(targetTextBox.Text);
                    dlg.Filter = filter;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        targetTextBox.Text = dlg.FileName;
                    }
                }
            }
        }

        private void BrowseGameConfigPath(object sender, EventArgs e)
        {
            BrowseFilePath(txtGameConfig, "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*");
        }

        private void BrowseOutputDirPath(object sender, EventArgs e)
        {
            BrowseFilePath(txtOutputDir, null, isFolderPicker: true);
        }

        private void BrowsePlaybackFilePath(object sender, EventArgs e)
        {
            BrowseFilePath(txtPlaybackFile, "JSON files (*.json)|*.json|All files (*.*)|*.*");
        }

        // Button State Functions
        private void SetRecordingButtonState(bool isRecording)
        {
            UpdateButtonState(
                btnRecord, true, 
                isRecording ? Color.Green : Color.White,
                isRecording ? Color.White : Color.Green
            );

            btnStop.Enabled = isRecording || IsPlaying;
        }

        private void SetPlaybackButtonState(bool isPlaying)
        {
            UpdateButtonState(
                btnPlay, true, 
                isPlaying ? Color.Green : Color.White, 
                isPlaying ? Color.White : Color.Green
            );

            btnStop.Enabled = IsRecording || IsPlaying;
        }

        private void SetPauseButtonState(bool isPaused)
        {
            UpdateButtonState(
                btnPause, true, 
                isPaused ? Color.Black : Color.White, 
                isPaused ? Color.White : Color.Black
            );
        }

        private void ResetButtonStates()
        {
            UpdateButtonState(btnRecord, true, Color.White, Color.Red);
            UpdateButtonState(btnPlay, true, Color.White, Color.Green);
            UpdateButtonState(btnPause, true, Color.White, Color.Black);
            UpdateButtonState(btnStop, true, Color.White, Color.Black);
        }

        private void UpdateButtonState(Button button, bool enabled, Color backColor, Color foreColor)
        {
            button.Enabled = enabled;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
        }

        // Config

        private void RegisterControlEvents()
        {
            foreach (Control control in this.Controls)
            {
                RegisterControlEvent(control);
            }
        }

        private void RegisterControlEvent(Control control)
        {
            if (control is TextBox textBox)
            {
                textBox.TextChanged += ControlValueChanged;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.CheckedChanged += ControlValueChanged;
            }
            else if (control is NumericUpDown numericUpDown)
            {
                numericUpDown.ValueChanged += ControlValueChanged;
            }

            foreach (Control child in control.Controls)
            {
                RegisterControlEvent(child);
            }
        }

        private void ControlValueChanged(object sender, EventArgs e)
        {
            // Save the changed control's state
            Control control = sender as Control;
            SaveControlState(control);
            _configManager.SaveConfig();
        }

        private void SaveControlStates()
        {
            foreach (Control control in this.Controls)
            {
                if (IgnoreConfig.Contains(control))
                    continue;
                SaveControlState(control);
            }

            _configManager.SaveConfig();
        }

        private void SaveControlState(Control control)
        {
            // Skip controls without a name
            if (string.IsNullOrEmpty(control.Name))
                return;

            string key = control.Name;
            object value = null;

            switch (control)
            {
                case TextBox textBox:
                    value = textBox.Text;
                    break;
                case CheckBox checkBox:
                    value = checkBox.Checked;
                    break;
                case NumericUpDown numericUpDown:
                    value = numericUpDown.Value;
                    break;
                default:
                    break;
            }

            if (value != null)
            {
                _configManager.Config[key] = value;
            }

            foreach (Control child in control.Controls)
            {
                SaveControlState(child);
            }
        }

        private void LoadControlStates()
        {
            foreach (Control control in this.Controls)
            {
                if (IgnoreConfig.Contains(control))
                    continue;
                LoadControlState(control);
            }
        }

        private void LoadControlState(Control control)
        {
            // Skip controls without a name
            if (string.IsNullOrEmpty(control.Name))
                return;

            string key = control.Name;

            if (_configManager.Config.TryGetValue(key, out object value))
            {
                switch (control)
                {
                    case TextBox textBox:
                        textBox.Text = value as string;
                        break;
                    case CheckBox checkBox:
                        if (value is bool boolValue)
                            checkBox.Checked = boolValue;
                        break;
                    case NumericUpDown numericUpDown:
                        if (value is decimal decimalValue)
                        numericUpDown.Value = decimalValue;
                        break;
                    default:
                        break;
                }
            }

            foreach (Control child in control.Controls)
            {
                LoadControlState(child);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _recordingPlaybackManager?.Dispose();
            SaveControlStates();
            UnregisterGlobalHotKeys();
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _recordingPlaybackManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}