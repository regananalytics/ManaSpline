namespace ManaSpline
{
    public interface IMouseSmoothingAlgorithm
    {
        (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY);
        void Reset();
    }

    public abstract class MouseSmoothingAlgorithmBase : IMouseSmoothingAlgorithm
    {
        public abstract (int SmoothedDeltaX, int SmoothedDeltaY) Add(int deltaX, int deltaY);
        public abstract void Reset();
    }
}