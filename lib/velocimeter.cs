public class Velocimeter
{
    public struct PositionSample
    {
        public Vector3D Position;
        public TimeSpan Timestamp;

        public PositionSample(Vector3D position, TimeSpan timestamp)
        {
            Position = position;
            Timestamp = timestamp;
        }
    }

    private readonly uint MaxSampleCount;
    private readonly LinkedList<PositionSample> Samples = new LinkedList<PositionSample>();

    public Velocimeter(uint maxSampleCount)
    {
        MaxSampleCount = maxSampleCount;
    }

    public void TakeSample(Vector3D position, TimeSpan timestamp)
    {
        var sample = new PositionSample(position, timestamp);
        Samples.AddLast(sample);
        // Age out old samples
        while (Samples.Count > MaxSampleCount)
        {
            Samples.RemoveFirst();
        }
    }

    public Vector3D? GetAverageVelocity()
    {
        // Need at least 2 samples...
        if (Samples.Count > 1)
        {
            var oldest = Samples.First;
            var newest = Samples.Last;
            var distance = newest.Value.Position - oldest.Value.Position;
            var seconds = newest.Value.Timestamp.TotalSeconds -
                oldest.Value.Timestamp.TotalSeconds;
            return distance / seconds;
        }
        return null;
    }

    public void Reset()
    {
        Samples.Clear();
    }
}
