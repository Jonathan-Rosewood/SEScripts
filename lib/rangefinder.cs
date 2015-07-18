public class Rangefinder
{
    // This comes from not being able to use Vector3I's constants due to whatever
    public static Vector3I Up = new Vector3I(0, 1, 0);
    public static Vector3I Down = new Vector3I(0, -1, 0);
    public static Vector3I Right = new Vector3I(1, 0, 0);
    public static Vector3I Left = new Vector3I(-1, 0, 0);
    public static Vector3I Forward = new Vector3I(0, 0, -1);
    public static Vector3I Backward = new Vector3I(0, 0, 1);

    public struct LineSample
    {
        public VRageMath.Vector3D Point;
        public VRageMath.Vector3D Direction;

        public LineSample(IMyCubeBlock reference, VRageMath.Vector3I forward3I)
        {
            Point = reference.GetPosition();
            var forwardPoint = reference.CubeGrid.GridIntegerToWorld(forward3I);
            Direction = VRageMath.Vector3D.Normalize(forwardPoint - Point);
        }

        public LineSample(IMyCubeBlock reference, IMyCubeBlock forward) :
            this(reference, forward.Position)
        {
        }

        public LineSample(IMyCubeBlock reference) :
            // reference.Orientation.Forward not accessible to us, so just assume grid Forward
            this(reference, reference.Position + Forward)
        {
        }
    }

    public static bool Compute(LineSample first, LineSample second, out VRageMath.Vector3D gps)
    {
        // It's really too bad. VRageMath.LineD implements this, but I believe
        // it only works on line segments.

        // Many thanks to http://geomalgorithms.com/a07-_distance.html :P
        var w0 = first.Point - second.Point;
        var a = VRageMath.Vector3D.Dot(first.Direction, first.Direction);
        var b = VRageMath.Vector3D.Dot(first.Direction, second.Direction);
        var c = VRageMath.Vector3D.Dot(second.Direction, second.Direction);
        var d = VRageMath.Vector3D.Dot(first.Direction, w0);
        var e = VRageMath.Vector3D.Dot(second.Direction, w0);

        var D = a * c - b * b;
        if (D > 0.0)
        {
            var sc = (b * e - c * d) / D;
            var tc = (a * e - b * d) / D;

            var pc = first.Point + sc * first.Direction;
            var qc = second.Point + tc * second.Direction;

            // We're interested in midpoint of pc-sc segment
            gps = (pc + qc) / 2.0;
            return true;
        }

        // Parallel lines
        gps = default(VRageMath.Vector3D);
        return false;
    }
}
