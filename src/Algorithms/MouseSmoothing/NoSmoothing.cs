namespace ManaSpline
{
    public class NoSmoothingAlgorithm : MouseSmoothingAlgorithmBase
    {
        public override (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY)
        {
            return (deltaX, deltaY);
        }

        public override void Reset() {}
    }
}