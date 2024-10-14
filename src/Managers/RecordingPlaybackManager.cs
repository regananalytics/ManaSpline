using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public class RecordingPlaybackManager
    {
        private MemManager _memManager;
        public StateRecorderPlayer _stateRecorderPlayer;
        public KeyRecorderPlayer _keyRecorderPlayer;
        public MouseRecorderPlayer _mouseRecorderPlayer;

        private CancellationTokenSource _cts;

        private bool _isRecording = false;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _disposed = false;

        public bool IsRecording => _isRecording;
        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;


        public RecordingPlaybackManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            _cts = new CancellationTokenSource();

            _memManager = new MemManager();
            ManaSpline.memManager = _memManager;

            bool isConnected = _memManager.Waiter.WaitForFlag(timeout: 5000);

            if (!isConnected) 
                throw new TimeoutException("MemManager failed to connect to the game.");

            _mouseRecorderPlayer = new MouseRecorderPlayer(_cts.Token);
            _keyRecorderPlayer = new KeyRecorderPlayer(_cts.Token);
            _stateRecorderPlayer = new StateRecorderPlayer(_cts.Token);

            ManaSpline.keyRecorderPlayer = _keyRecorderPlayer;
            ManaSpline.mouseRecorderPlayer = _mouseRecorderPlayer;
            ManaSpline.stateRecorderPlayer = _stateRecorderPlayer;
        }

        public void StartRecording()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RecordingPlaybackManager));

            // Reset the cancellation token if previously cancelled
            if (_cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();

            _isRecording = true;
            _isPlaying = false;
            _isPaused = false;

            ManaSpline.Log("Starting Recording");

            // Start recorders
            StartRecorder(_stateRecorderPlayer, (bool)ManaSpline.config["EnableStateRecording"]);
            StartRecorder(_keyRecorderPlayer, (bool)ManaSpline.config["EnableKeyRecording"]);
            StartRecorder(_mouseRecorderPlayer, (bool)ManaSpline.config["EnableMouseRecording"]);
        }

        public void StartPlayback()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RecordingPlaybackManager));

            // Reset the cancellation token if previously cancelled
            if (_cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();

            _isPlaying = true;
            _isRecording = false;
            _isPaused = false;

            ManaSpline.Log("Starting Playback");
            ManaSpline.FileReader.Start();

            // Start recorders
            PlayRecorder(_stateRecorderPlayer, (bool)ManaSpline.config["EnableStatePlayback"]);
            PlayRecorder(_keyRecorderPlayer, (bool)ManaSpline.config["EnableKeyPlayback"]);
            PlayRecorder(_mouseRecorderPlayer, (bool)ManaSpline.config["EnableMousePlayback"]);
        }

        public void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RecordingPlaybackManager));

            _cts.Cancel();

            ManaSpline.FileWriter?.CompleteAsync().Wait();
            ManaSpline.FileWriter?.Dispose();
            ManaSpline.FileReader?.Dispose();

            _isRecording = false;
            _isPlaying = false;
            _isPaused = false;

            // Stop all recorders
            StopRecorder(_stateRecorderPlayer);
            StopRecorder(_keyRecorderPlayer);
            StopRecorder(_mouseRecorderPlayer);
        }

        public void Pause()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RecordingPlaybackManager));

            _isPaused = true;

            // Pause Recorders
            PauseRecorder(_stateRecorderPlayer);
            PauseRecorder(_keyRecorderPlayer);
            PauseRecorder(_mouseRecorderPlayer);
        }

        public void Resume()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RecordingPlaybackManager));

            _isPaused = false;

            // Resume Recorders
            ResumeRecorder(_stateRecorderPlayer);
            ResumeRecorder(_keyRecorderPlayer);
            ResumeRecorder(_mouseRecorderPlayer);
        }

        private void StartRecorder(IRecorderPlayer recorderPlayer, bool isEnabled)
        {
            if (isEnabled)
                recorderPlayer.Start();
        }

        private void PlayRecorder(IRecorderPlayer recorderPlayer, bool isEnabled)
        {
            if (isEnabled)
                recorderPlayer.Play();
        }

        private void StopRecorder(IRecorderPlayer recorderPlayer)
        {
            recorderPlayer?.Stop();
        }

        private void PauseRecorder(IRecorderPlayer recorderPlayer)
        {
            if (recorderPlayer?.IsRunning == true)
                recorderPlayer.Pause();
        }

        private void ResumeRecorder(IRecorderPlayer recorderPlayer)
        {
            if (recorderPlayer?.IsRunning == true && recorderPlayer.IsPaused)
                recorderPlayer.Resume();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();

                _cts?.Dispose();
                _memManager?.Dispose();

                _stateRecorderPlayer?.Dispose();
                _keyRecorderPlayer?.Dispose();
                _mouseRecorderPlayer?.Dispose();

                _disposed = true;
            }
        }
    }
}