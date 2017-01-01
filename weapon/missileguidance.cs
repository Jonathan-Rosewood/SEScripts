//@ shipcontrol eventdriver seeker velocimeter pid
public class MissileGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private const double ThrustKp = 1.0;
    private const double ThrustTi = 5.0;
    private const double ThrustTd = 0.3;
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const bool PerturbTarget = true;
    private const double PerturbAmplitude = 5000.0;
    private const double PerturbAmplitudeScale = 10.0;
    private const double PerturbTimeScale = 5.0;
    private const double PerturbOffset = 200.0;

    private const double ManeuveringSpeed = 80.0; // In meters per second
    private const float ManeuveringRoll = 10.0f; // In RPM

    private const double FinalApproachDistance = 200.0;
    private const float FinalApproachRoll = MathHelper.Pi;

    private const double DetonationDistance = 30.0;
    private readonly TimeSpan DetonationTime = TimeSpan.FromSeconds(200.0);

    private Vector3D Target;
    private double RandomOffset;
    private bool FinalApproach = false;

    public MissileGuidance()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ti = ThrustTi;
        thrustPID.Td = ThrustTd;
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
        var panel = panels[0]; // Just use the first one
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

        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        shipControl.GyroControl.SetAxisVelocityRPM(GyroControl.Roll, ManeuveringRoll);

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

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        if (!FinalApproach)
        {
            if (distance < FinalApproachDistance)
            {
                FinalApproach = true;
                shipControl.GyroControl.SetAxisVelocity(GyroControl.Roll, FinalApproachRoll);
                shipControl.ThrustControl.SetOverride(Base6Directions.Direction.Forward);
            }
            else
            {
                Maneuver(shipControl, eventDriver);
            }
        }

//        if (distance < DetonationDistance || eventDriver.TimeSinceStart >= DetonationTime)
//        {
//            // Sensor should have triggered already, just detonate/self-destruct
//            var warheads = ZACommons.GetBlocksOfType<IMyWarhead>(commons.Blocks);
//            warheads.ForEach(warhead => warhead.GetActionWithName("Detonate").Apply(warhead));
//        }

        eventDriver.Schedule(FramesPerRun, Run);
    }

    private double Perturb(ShipControlCommons shipControl, TimeSpan timeSinceStart, out Vector3D targetVector)
    {
        targetVector = Target - shipControl.ReferencePoint;
        var distance = targetVector.Normalize(); // Original distance
        var amp = ScaleAmplitude(distance);
        var newTarget = Target;
        newTarget += shipControl.ReferenceUp * amp * Math.Cos(PerturbTimeScale * timeSinceStart.TotalSeconds + RandomOffset);
        newTarget += shipControl.ReferenceLeft * amp * Math.Sin(PerturbTimeScale * timeSinceStart.TotalSeconds + RandomOffset);
        targetVector = Vector3D.Normalize(newTarget - shipControl.ReferencePoint);
        return distance;
    }

    private double ScaleAmplitude(double distance)
    {
        distance -= PerturbOffset;
        // Try linear for now
        distance = Math.Max(distance, 0.0);
        distance = Math.Min(distance, PerturbAmplitude * PerturbAmplitudeScale);
        return distance / PerturbAmplitudeScale;
    }

    private void Maneuver(ShipControlCommons shipControl, EventDriver eventDriver)
    {
        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            // Only absolute velocity
            var speed = ((Vector3D)velocity).Length();
            var error = ManeuveringSpeed - speed;

            var force = thrustPID.Compute(error);

            var thrustControl = shipControl.ThrustControl;
            if (force > 0.0)
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, force);
            }
            else
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, false);
            }
        }
    }
}
