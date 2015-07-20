public class Rangefinder
{
    public struct LineSample
    {
        public Vector3D Point;
        public Vector3D Direction;

        public LineSample(IMyCubeBlock reference, Vector3I forward3I)
        {
            Point = reference.GetPosition();
            var forwardPoint = reference.CubeGrid.GridIntegerToWorld(forward3I);
            Direction = Vector3D.Normalize(forwardPoint - Point);
        }

        public LineSample(IMyCubeBlock reference, IMyCubeBlock forward) :
            this(reference, forward.Position)
        {
        }

        // Use the "forward" direction of the reference block
        public LineSample(IMyCubeBlock reference) :
            this(reference, reference.Position + Base6Directions.GetIntVector(reference.Orientation.TransformDirection(Base6Directions.Direction.Forward)))
        {
        }
    }

    public static bool Compute(LineSample first, LineSample second, out Vector3D target)
    {
        // It's really too bad. VRageMath.LineD implements this, but I believe
        // it only works on line segments.

        // Many thanks to http://geomalgorithms.com/a07-_distance.html :P
        var w0 = first.Point - second.Point;
        var a = Vector3D.Dot(first.Direction, first.Direction);
        var b = Vector3D.Dot(first.Direction, second.Direction);
        var c = Vector3D.Dot(second.Direction, second.Direction);
        var d = Vector3D.Dot(first.Direction, w0);
        var e = Vector3D.Dot(second.Direction, w0);

        var D = a * c - b * b;
        if (D > 0.0)
        {
            var sc = (b * e - c * d) / D;
            var tc = (a * e - b * d) / D;

            var pc = first.Point + sc * first.Direction;
            var qc = second.Point + tc * second.Direction;

            // We're interested in midpoint of pc-sc segment
            target = (pc + qc) / 2.0;
            return true;
        }

        // Parallel lines
        target = default(Vector3D);
        return false;
    }
}
