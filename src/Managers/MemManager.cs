using MemCore;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ManaSpline
{
    public class MemManager : IDisposable
    {
        private string _configPath;
        private CancellationTokenSource _cts;
        private readonly BooleanWaiter _waiter = new BooleanWaiter();
        public BooleanWaiter Waiter => _waiter;

        public MemoryCore MemCore;

        private readonly object _lock = new object();

        private Stopwatch _stopwatch;
        private long _syncIntervalMs = Math.Min(50, Convert.ToInt32(ManaSpline.config["StateInterval"]));

        private float _lastIGTSync = 0;
        private volatile bool _isGamePaused;
        private readonly AsyncConditionWaiter _pauseWaiter;
        private Dictionary<string, object> _lastGameState = null;

        public bool IsConnected { get; private set; } = false;

        public bool IsGamePaused
        {
            get => _isGamePaused;
            private set
            {
                if (_isGamePaused != value)
                {
                    _isGamePaused = value;
                    _pauseWaiter.SetCondition(value);
                }
            }
        }

        public Dictionary<string, object> LastGameState
        {
            get
            {
                lock (_lock)
                {
                    return _lastGameState != null ? new Dictionary<string, object>(_lastGameState) : null;
                }
            }
        }

        public MemManager()
        {
            _configPath = (string)ManaSpline.config["GameConfig"];
            _cts = new CancellationTokenSource();
            _stopwatch = Stopwatch.StartNew();
            _pauseWaiter = new AsyncConditionWaiter(initialCondition: false);

            Task.Run(() => InitMemCore());
        }

        private async Task InitMemCore()
        {
            while (!_cts.Token.IsCancellationRequested && MemCore == null)
            {
                try
                {
                    MemCore = new MemoryCore(_configPath, false);

                    IsConnected = true;
                    _waiter.SetFlag();

                    ManaSpline.Log("Connected to Game!");

                    _lastIGTSync = GetGameIGT();

                    // Start the background task to update IGT
                    Task.Run(() => UpdateIGTLoop(), _cts.Token);

                    break;
                }
                catch (InvalidOperationException)
                {
                    // Process not available, keep waiting.
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error occured while connecting to game: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                    _waiter.SetFlag();
                    return;
                }
            }
        }

        public Dictionary<string, object> GetGameState()
        {
            return MemCore?.GetState();
        }

        public float GetGameIGT(Dictionary<string, object> state = null)
        {
            if (state == null)
                state = GetGameState();

            if (state != null && state.ContainsKey("IGT"))
                return (float)(state["IGT"] ?? 0f);
            else
                return 0;
        }

        public float GetIGT()
        {
            (float igt, float delta) = GetIGTDelta();
            return igt + delta;
        }

        public (float igt, float delta) GetIGTDelta()
        {
            if (MemCore != null)
                return (_lastIGTSync, (float)_stopwatch.ElapsedMilliseconds / 1000.0f);
            else
                return (0f, 0f);
        }

        private async Task UpdateIGTLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var newState = GetGameState();
                float newIGT = GetGameIGT(newState);

                // Determine if the game is paused
                IsGamePaused = newIGT == _lastIGTSync;

                // Update the last synced IGT
                _lastIGTSync = newIGT;
                _lastGameState = newState;
                _stopwatch.Restart();

                await Task.Delay((int)_syncIntervalMs, _cts.Token);
            }
        }

        public async Task WaitForUnpasuedAsync(CancellationToken cancellationToken = default)
        {
            if (IsGamePaused)
            {
                await _pauseWaiter.WaitUntilAsync(desiredValue: false, cancellationToken);
                ManaSpline.Log("Unpaused!");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _waiter.Dispose();

            if (MemCore != null)
            {
                // MemCore.Dispose();
                MemCore = null;
            }
        }
    }
}