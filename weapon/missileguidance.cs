public class MissileGuidance
{
    public static Vector3I Up3I = new Vector3I(0, 1, 0);
    public static Vector3I Right3I = new Vector3I(1, 0, 0);

    public struct Orientation
    {
        public Vector3D Point;
        public Vector3D Up;
        public Vector3D Right;

        public Orientation(IMyCubeBlock reference)
        {
            Point = reference.GetPosition();
            var up3I = reference.Position + Up3I;
            Up = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(up3I) - Point);
            var right3I = reference.Position + Right3I;
            Right = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(right3I) - Point);
        }
    }

    private const uint TicksPerRun = 5;

    private uint Tick;
    private Vector3D Target = new Vector3D(-112.20, -4.79, -202.81);
    private TimeSpan TotalElapsed = TimeSpan.FromSeconds(0);

    private const double PerturbAmplitude = 5000.0;
    private const double PerturbScale = 3.0;

    private double ScaleAmplitude(double distance)
    {
        distance -= 200.0;
        distance = Math.Max(distance, 0.0);
        distance = Math.Min(distance, PerturbAmplitude);
        // Try linear for now
        return PerturbAmplitude * distance / PerturbAmplitude;
    }

    private double Perturb(Orientation orientation, out Vector3D targetVector)
    {
        targetVector = Target - orientation.Point;
        var distance = targetVector.Normalize(); // Original distance
        var amp = ScaleAmplitude(distance);
        var newTarget = Target;
//        newTarget += orientation.Up * amp * Math.Sin(PerturbScale * distance);
        newTarget += orientation.Right * amp * Math.Sin(PerturbScale * TotalElapsed.TotalSeconds);
        targetVector = Vector3D.Normalize(newTarget - orientation.Point);
        return distance;
    }

    public void Run(MyGridProgram program)
    {
        TotalElapsed += program.ElapsedTime;

        if (Tick % TicksPerRun == 0)
        {
            // TODO thruster stuff doesn't belong here
            var forwardGroup = ZALibrary.GetBlockGroupWithName(program, "CM Forward");
            if (forwardGroup == null)
            {
                throw new Exception("Missing group: CM Forward");
            }
            ZAFlightLibrary.SetThrusterOverride(forwardGroup.Blocks, 12000.0f);

            var ship = new List<IMyTerminalBlock>();
            program.GridTerminalSystem.GetBlocks(ship);

            var gyros = ZALibrary.GetBlocksOfType<IMyGyro>(ship,
                                                           test => test.IsFunctional && test.IsWorking);
            if (gyros.Count != 1) return;
            var gyro = gyros[0];

            var orientation = new Orientation(program.Me);

            Vector3D targetVector;
            var distance = Perturb(orientation, out targetVector);
            var yaw = Vector3D.Dot(targetVector, orientation.Right);
            var pitch = Vector3D.Dot(targetVector, orientation.Up);

            ZAFlightLibrary.EnableGyroOverride(gyro, true);
            ZAFlightLibrary.SetAxisVelocity(gyro, ZAFlightLibrary.GyroAxisYaw, (float)(yaw * 3.14));
            ZAFlightLibrary.SetAxisVelocity(gyro, ZAFlightLibrary.GyroAxisPitch, (float)(pitch * 3.14));
        }

        Tick++;
    }
}
