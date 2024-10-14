using System;

namespace ManaSpline
{
    public class KalmanFilterSmoothing : MouseSmoothingAlgorithmBase
    {
        private double _estimateX;
        private double _estimateY;
        private double _errorEstimateX;
        private double _errorEstimateY;
        private readonly double _processNoise;
        private readonly double _measurementNoise;

        public KalmanFilterSmoothing(double processNoise = 1e-5, double measurementNoise = 1e-2)
        {
            _processNoise = processNoise;
            _measurementNoise = measurementNoise;
            _errorEstimateX = 1;
            _errorEstimateY = 1;
            _estimateX = 0;
            _estimateY = 0;
        }

        public override (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY)
        {
            double priorEstimateX = _estimateX;
            double priorErrorEstimateX = _errorEstimateX + _processNoise;

            double kalmanGainX = priorErrorEstimateX / (priorErrorEstimateX + _measurementNoise);
            _estimateX = priorEstimateX + kalmanGainX * (deltaX - priorEstimateX);
            _errorEstimateX = (1 - kalmanGainX) * priorErrorEstimateX;

            double priorEstimateY = _estimateY;
            double priorErrorEstimateY = _errorEstimateY + _processNoise;

            double kalmanGainY = priorErrorEstimateY / (priorErrorEstimateY + _measurementNoise);
            _estimateY = priorEstimateY + kalmanGainY * (deltaY - priorEstimateY);
            _errorEstimateY = (1 - kalmanGainY) * priorErrorEstimateY;

            return ((int)Math.Round(_estimateX), (int)Math.Round(_estimateY));
        }

        public override void Reset()
        {
            _estimateX = 0;
            _estimateY = 0;
            _errorEstimateX = 1;
            _errorEstimateY = 1;
        }
    }
}