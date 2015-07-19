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

    private const uint FramesPerRun = 5;

    private Vector3D Target;
    private double RandomOffset;

    private const double GyroKp = 5.0; // Proportional constant
    private const double PerturbAmplitude = 5000.0;
    private const double PerturbPitchScale = 1.0;
    private const double PerturbYawScale = 1.0;
    private const double PerturbScale = 3.0;
    private const double PerturbOffset = 400.0;
    private const double FinalApproachDistance = 300.0; // Should be < PerturbOffset

    private double ScaleAmplitude(double distance)
    {
        distance -= PerturbOffset;
        distance = Math.Max(distance, 0.0);
        distance = Math.Min(distance, PerturbAmplitude);
        // Try linear for now
        return PerturbAmplitude * distance / PerturbAmplitude;
    }

    private double Perturb(TimeSpan timeSinceStart, Orientation orientation, out Vector3D targetVector)
    {
        targetVector = Target - orientation.Point;
        var distance = targetVector.Normalize(); // Original distance
        var amp = ScaleAmplitude(distance);
        var newTarget = Target;
        newTarget += orientation.Up * amp * Math.Cos(PerturbScale * timeSinceStart.TotalSeconds + RandomOffset) * PerturbPitchScale;
        newTarget += orientation.Right * amp * Math.Sin(PerturbScale * timeSinceStart.TotalSeconds + RandomOffset) * PerturbYawScale;
        targetVector = Vector3D.Normalize(newTarget - orientation.Point);
        return distance;
    }

    public void AcquireTarget(MyGridProgram program)
    {
        // Find the sole text panel
        var panelGroup = ZALibrary.GetBlockGroupWithName(program, "CM Target");
        if (panelGroup == null)
        {
            throw new Exception("Missing group: CM Target");
        }

        var panels = ZALibrary.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0)
        {
            throw new Exception("Expecting at least 1 text panel");
        }
        var panel = panels[0] as IMyTextPanel; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 3)
        {
            throw new Exception("Expecting exactly 3 parts to target info");
        }
        Target = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            Target.SetDim(i, double.Parse(parts[i]));
        }
    }

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        // Randomize in case of simultaneous launch with other missiles
        Random random = new Random(this.GetHashCode());
        RandomOffset = 1000.0 * random.NextDouble();

        eventDriver.Schedule(0, Run);
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        var ship = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocks(ship);

        var gyros = ZALibrary.GetBlocksOfType<IMyGyro>(ship,
                                                       test => test.IsFunctional && test.IsWorking);
        if (gyros.Count != 1) return;
        var gyro = gyros[0];

        var orientation = new Orientation(program.Me);

        Vector3D targetVector;
        var distance = Perturb(eventDriver.TimeSinceStart, orientation, out targetVector);
        var yaw = Vector3D.Dot(targetVector, orientation.Right);
        var pitch = Vector3D.Dot(targetVector, orientation.Up);

        ZAFlightLibrary.EnableGyroOverride(gyro, true);
        ZAFlightLibrary.SetAxisVelocity(gyro, ZAFlightLibrary.GyroAxisYaw, (float)(yaw * GyroKp));
        ZAFlightLibrary.SetAxisVelocity(gyro, ZAFlightLibrary.GyroAxisPitch, (float)(pitch * GyroKp));

        if (distance < FinalApproachDistance)
        {
            ZAFlightLibrary.SetAxisVelocity(gyro, ZAFlightLibrary.GyroAxisRoll, MathHelper.Pi);
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
