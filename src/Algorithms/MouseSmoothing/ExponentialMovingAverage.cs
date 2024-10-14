using System;

namespace ManaSpline
{
    public class ExponentialMovingAverageSmoothing : MouseSmoothingAlgorithmBase
    {
        private readonly double _alpha;
        private double _emaDeltaX;
        private double _emaDeltaY;
        private bool _initialized;

        public ExponentialMovingAverageSmoothing(double alpha)
        {
            if (alpha <= 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1");
            
            _alpha = alpha;
            _initialized = false;
        }

        public override (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY)
        {
            if (!_initialized)
            {
                _emaDeltaX = deltaX;
                _emaDeltaY = deltaY;
                _initialized = true;
            }
            else
            {
                _emaDeltaX = _alpha * deltaX + (1 - _alpha) * _emaDeltaX;
                _emaDeltaY = _alpha * deltaY + (1 - _alpha) * _emaDeltaY;
            }

            return ((int)Math.Round(_emaDeltaX), (int)Math.Round(_emaDeltaY));
        }

        public override void Reset()
        {
            _initialized = false;
            _emaDeltaX = 0;
            _emaDeltaY = 0;
        }
    }
}