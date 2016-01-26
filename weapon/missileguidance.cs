public class MissileGuidance
{
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

    private double Perturb(ShipControlCommons shipControl, TimeSpan timeSinceStart, out Vector3D targetVector)
    {
        targetVector = Target - shipControl.ReferencePoint;
        var distance = targetVector.Normalize(); // Original distance
        var amp = ScaleAmplitude(distance);
        var newTarget = Target;
        newTarget += shipControl.ReferenceUp * amp * Math.Cos(PerturbScale * timeSinceStart.TotalSeconds + RandomOffset) * PerturbPitchScale;
        newTarget += shipControl.ReferenceLeft * amp * Math.Sin(PerturbScale * timeSinceStart.TotalSeconds + RandomOffset) * PerturbYawScale;
        targetVector = Vector3D.Normalize(newTarget - shipControl.ReferencePoint);
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

        Vector3D targetVector;
        double distance;
        if (PerturbTarget)
        {
            distance = Perturb(shipControl, eventDriver.TimeSinceStart, out targetVector);
        }
        else
        {
            targetVector = Target - shipControl.ReferencePoint;
            distance = targetVector.Normalize();
        }

        // Determine projection of targetVector onto our reference unit vectors
        var dotZ = targetVector.Dot(shipControl.ReferenceForward);
        var dotX = targetVector.Dot(shipControl.ReferenceLeft);
        var dotY = targetVector.Dot(shipControl.ReferenceUp);

        var projZ = dotZ * shipControl.ReferenceForward;
        var projX = dotX * shipControl.ReferenceLeft;
        var projY = dotY * shipControl.ReferenceUp;

        // Determine yaw/pitch error by calculating angle between our forward
        // vector and targetVector
        var yawError = Math.Atan(projX.Length() / projZ.Length());
        var pitchError = Math.Atan(projY.Length() / projZ.Length());

        // Set sign according to sign of original dot product
        yawError *= Math.Sign(dotX);
        pitchError *= Math.Sign(-dotY); // NB flipped

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        // Constraining doesn't seem necessary...

        /*
        if (Math.Abs(gyroYaw) > GyroMaxRadiansPerSecond) gyroYaw = GyroMaxRadiansPerSecond * Math.Sign(gyroYaw);
        if (Math.Abs(gyroPitch) > GyroMaxRadiansPerSecond) gyroPitch = GyroMaxRadiansPerSecond * Math.Sign(gyroPitch);
        */

        /*
        if (Math.Abs(gyroYaw) + Math.Abs(gyroPitch) > GyroMaxRadiansPerSecond)
        {
            var adjust = GyroMaxRadiansPerSecond / (Math.Abs(gyroYaw) + Math.Abs(gyroPitch));
            gyroYaw *= adjust;
            gyroPitch *= adjust;
        }
        */

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
