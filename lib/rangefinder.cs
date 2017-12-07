public class Rangefinder
{
    public struct LineSample
    {
        public Vector3D Point;
        public Vector3D Direction;

        // Use the "forward" direction of the reference block
        public LineSample(IMyCubeBlock reference)
        {
            Point = reference.GetPosition();
            Direction = reference.WorldMatrix.Forward;
        }

        // Explicit position & direction
        public LineSample(Vector3D position, Vector3D direction)
        {
            Point = position;
            Direction = Vector3D.Normalize(direction);
        }
    }

    public static bool Compute(LineSample first, LineSample second,
                               out Vector3D closestFirst,
                               out Vector3D closestSecond)
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

            // Closest point on first line
            closestFirst = first.Point + sc * first.Direction;
            // Closest point on second line
            closestSecond = second.Point + tc * second.Direction;
            return true;
        }

        // Parallel lines
        closestFirst = default(Vector3D);
        closestSecond = default(Vector3D);
        return false;
    }
}
