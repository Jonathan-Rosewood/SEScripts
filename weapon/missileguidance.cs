public class MissileGuidance
{
    public static Vector3D Zero3D = new Vector3D();
    public static Vector3D Forward3D = new Vector3D(0.0, 0.0, 1.0);

    public struct Orientation
    {
        public Vector3D Point;
        public Vector3D Forward;
        public Vector3D Up;
        public Vector3D Left;

        public Orientation(IMyCubeBlock reference,
                           Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                           Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
        {
            Point = reference.GetPosition();
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipForward);
            Forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - Point);
            var up3I = reference.Position + Base6Directions.GetIntVector(shipUp);
            Up = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(up3I) - Point);
            var left3I = reference.Position + Base6Directions.GetIntVector(Base6Directions.GetLeft(shipUp, shipForward));
            Left = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(left3I) - Point);
        }
    }

    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private const double GyroMaxRadiansPerSecond = Math.PI; // Really pi*2, but something's odd...

    private Vector3D Target;
    private double RandomOffset;

    private const bool PerturbTarget = true;
    private const double PerturbAmplitude = 5000.0;
    private const double PerturbPitchScale = 1.0;
    private const double PerturbYawScale = 1.0;
    private const double PerturbScale = 3.0;
    private const double PerturbOffset = 200.0;
    private const double FinalApproachDistance = 200.0;
    private const float FinalApproachRoll = MathHelper.Pi;
    private const double DetonationDistance = 30.0;
    private readonly TimeSpan DetonationTime = TimeSpan.FromSeconds(200.0);

    private const double GyroKp = 250.0; // Proportional constant
    private const double GyroKi = 0.0; // Integral constant
    private const double GyroKd = 200.0; // Derivative constant
    private readonly PIDController yawPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController pitchPID = new PIDController(1.0 / RunsPerSecond);

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
        newTarget += orientation.Left * amp * Math.Sin(PerturbScale * timeSinceStart.TotalSeconds + RandomOffset) * PerturbYawScale;
        targetVector = Vector3D.Normalize(newTarget - orientation.Point);
        return distance;
    }

    public void AcquireTarget(ZACommons commons)
    {
        // Find the sole text panel
        var panelGroup = commons.GetBlockGroupWithName("CM Target");
        if (panelGroup == null)
        {
            throw new Exception("Missing group: CM Target");
        }

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
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

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        // Randomize in case of simultaneous launch with other missiles
        Random random = new Random(this.GetHashCode());
        RandomOffset = 1000.0 * random.NextDouble();

        yawPID.Kp = GyroKp;
        yawPID.Ki = GyroKi;
        yawPID.Kd = GyroKd;

        pitchPID.Kp = GyroKp;
        pitchPID.Ki = GyroKi;
        pitchPID.Kd = GyroKd;

        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var orientation = new Orientation(commons.Me,
                                          shipUp: shipControl.ShipUp,
                                          shipForward: shipControl.ShipForward);

        Vector3D targetVector;
        double distance;
        if (PerturbTarget)
        {
            distance = Perturb(eventDriver.TimeSinceStart, orientation, out targetVector);
        }
        else
        {
            targetVector = Target - orientation.Point;
            distance = targetVector.Normalize();
        }

        // Transform relative to our forward vector
        targetVector = Vector3D.Transform(targetVector, MatrixD.CreateLookAt(Zero3D, -orientation.Forward, orientation.Up));

        var yawVector = new Vector3D(targetVector.GetDim(0), 0, targetVector.GetDim(2));
        var pitchVector = new Vector3D(0, targetVector.GetDim(1), targetVector.GetDim(2));
        yawVector.Normalize();
        pitchVector.Normalize();

        var yawError = Math.Acos(Vector3D.Dot(yawVector, Forward3D)) * Math.Sign(targetVector.GetDim(0));
        var pitchError = -Math.Acos(Vector3D.Dot(pitchVector, Forward3D)) * Math.Sign(targetVector.GetDim(1));

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        if (Math.Abs(gyroYaw) + Math.Abs(gyroPitch) > GyroMaxRadiansPerSecond)
        {
            var adjust = GyroMaxRadiansPerSecond / (Math.Abs(gyroYaw) + Math.Abs(gyroPitch));
            gyroYaw *= adjust;
            gyroPitch *= adjust;
        }

        var gyroControl = shipControl.GyroControl;
        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        if (distance < FinalApproachDistance)
        {
            gyroControl.SetAxisVelocity(GyroControl.Roll, FinalApproachRoll);
        }
        if (distance < DetonationDistance || eventDriver.TimeSinceStart >= DetonationTime)
        {
            // Sensor should have triggered already, just detonate/self-destruct
            var warheads = ZACommons.GetBlocksOfType<IMyWarhead>(commons.Blocks);
            warheads.ForEach(warhead => warhead.GetActionWithName("Detonate").Apply(warhead));
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
