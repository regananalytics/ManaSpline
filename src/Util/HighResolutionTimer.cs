using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ManaSpline
{
    public class TimerState
    {
        public List<(int x, int y)> DataList { get; set; }
        public int CurrentIndex { get; set; } = 0;
        public Action <int, int> Action{ get; set; }
    }

    public class HighResolutionTimer : IDisposable
    {
        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeSetEvent(
            uint delay, 
            uint resolution, 
            TimeProc userCallback,
            IntPtr userCtx, 
            uint eventType
        );

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeKillEvent(uint timerEventId);

        private delegate void TimeProc(uint id, uint msg, IntPtr userCtx, IntPtr rsv1, IntPtr rsv2);

        private uint _timerId;
        private TimeProc _timeProc;
        private GCHandle _gcHandle;

        private List<(int x, int y)> _dataList;
        private int _currentIndex = 0;
        private Action<int, int> _action;
        private uint _interval;

        public HighResolutionTimer(Action<int, int> action)
        {
            _action = action;
            _timeProc = new TimeProc(TimerCallback);

            // Prevent the state object from being garbage collected
            _gcHandle = GCHandle.Alloc(this);
        }

        public void Start(List<(int x, int y)> dataList, uint interval)
        {
            _dataList = dataList;
            _interval = interval;

            // Convert the GCHandle to an IntPtr to pass as user context
            IntPtr userCtx = GCHandle.ToIntPtr(_gcHandle);

            _timerId = timeSetEvent(
                interval,
                0,
                _timeProc,
                userCtx,
                1 // TIME_PERIODIC for periodic execution
            );

            if (_timerId == 0)
                throw new InvalidOperationException("Unable to start multimedia timer.");
        }

        private void TimerCallback(uint id, uint msg, IntPtr userCtx, IntPtr rsv1, IntPtr rsv2)
        {
            // Retrieve the TimerState from userCtx
            var gch = GCHandle.FromIntPtr(userCtx);
            var timerInstance = (HighResolutionTimer)gch.Target;

            // Execute the action with the next tuple
            if (timerInstance._currentIndex < timerInstance._dataList.Count)
            {
                var (x, y) = timerInstance._dataList[_currentIndex];
                timerInstance._action(x, y);
                timerInstance._currentIndex++;
            }
            else
            {
                timerInstance.Stop();
            }
        }

        public void Stop()
        {
            if (_timerId != 0)
            {
                timeKillEvent(_timerId);
                _timerId = 0;
            }

            if (_gcHandle.IsAllocated)
                _gcHandle.Free();
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        ~HighResolutionTimer()
        {
            Stop();
        }
    }
}