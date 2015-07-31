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
            var last = Samples.First;
            Vector3D distance = new Vector3D();
            double seconds = 0.0;
            for (var current = last.Next;
                 current != null;
                 current = current.Next)
            {
                distance += current.Value.Position - last.Value.Position;
                seconds += current.Value.Timestamp.TotalSeconds -
                    last.Value.Timestamp.TotalSeconds;
                last = current;
            }

            return distance / seconds;
        }
        return null;
    }

    public void Reset()
    {
        Samples.Clear();
    }
}
