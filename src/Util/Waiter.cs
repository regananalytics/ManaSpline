using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public class BooleanWaiter : IDisposable
    {
        private readonly ManualResetEventSlim _event = new ManualResetEventSlim(false);
        private bool _disposed = false;

        public void SetFlag()
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(BooleanWaiter));
            _event.Set();
        }

        public void ResetFlag()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BooleanWaiter));
            _event.Reset();
        }

        public bool WaitForFlag(int timeout = Timeout.Infinite)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BooleanWaiter));
            return _event.Wait(timeout);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _event.Dispose();
                _disposed = true;
            }
        }
    }

    public class AsyncConditionWaiter
    {
        private volatile bool _condition;
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

        public AsyncConditionWaiter(bool initialCondition)
        {
            _condition = initialCondition;
        }

        // Wait asynchronously until the condition becomes the desired value
        public Task WaitUntilAsync(bool desiredValue, CancellationToken cancellationToken = default)
        {
            if (_condition == desiredValue)
            {
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

                // Capture the current TCS
                var previousTcs = Interlocked.Exchange(ref _tcs, tcs);
                
                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
                }

                // Return the new task
                return tcs.Task;
            }
        }

        // Sets the condition and notifies waiting tasks if the condition has changed
        public void SetCondition(bool newValue)
        {
            if (_condition != newValue)
            {
                _condition = newValue;

                // Capture and replace the TCS atomically
                var tcsToSet = Interlocked.Exchange(ref _tcs, new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously));

                // Set the result outside of any lock
                tcsToSet.TrySetResult(true);
            }
        }

        public bool GetCondition() => _condition;
    }
}