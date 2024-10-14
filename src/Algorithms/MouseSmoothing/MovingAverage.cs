using System.Collections.Generic;

namespace ManaSpline
{
    public class MovingAverageSmoothing : MouseSmoothingAlgorithmBase
    {
        private readonly int _windowSize;
        private readonly Queue<int> _deltaXValues;
        private readonly Queue<int> _deltaYValues;
        private int _sumDeltaX;
        private int _sumDeltaY;

        public MovingAverageSmoothing(int windowSize)
        {
            _windowSize = windowSize;
            _deltaXValues = new Queue<int>(windowSize);
            _deltaYValues = new Queue<int>(windowSize);
            _sumDeltaX = 0;
            _sumDeltaY = 0;
        }

        public override (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY)
        {
            _deltaXValues.Enqueue(deltaX);
            _deltaYValues.Enqueue(deltaY);
            _sumDeltaX += deltaX;
            _sumDeltaY += deltaY;

            if (_deltaXValues.Count > _windowSize)
            {
                _sumDeltaX -= _deltaXValues.Dequeue();
            }

            if (_deltaYValues.Count > _windowSize)
            {
                _sumDeltaY -= _deltaYValues.Dequeue();
            }

            int smoothedDeltaX = _sumDeltaX / _deltaXValues.Count;
            int smoothedDeltaY = _sumDeltaY / _deltaYValues.Count;

            return (smoothedDeltaX, smoothedDeltaY);
        }
        
        public override void Reset()
        {
            _deltaXValues.Clear();
            _deltaYValues.Clear();
            _sumDeltaX = 0;
            _sumDeltaY = 0;
        }
    }
}