public class ReverseThrust
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private const double GyroKp = 250.0; // Proportional constant
    private const double GyroKi = 0.0; // Integral constant
    private const double GyroKd = 200.0; // Derivative constant
    private readonly PIDController yawPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController pitchPID = new PIDController(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private int SampleCount;

    private Base6Directions.Direction ThrusterDirection;
    private bool Enabled;
    private Vector3D TargetVector;

    private Base6Directions.Direction LocalForward, LocalUp, LocalLeft;

    private double MaxGyroError;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Base6Directions.Direction thrusterDirection = Base6Directions.Direction.Forward)
    {
        ThrusterDirection = thrusterDirection;

        yawPID.Kp = GyroKp;
        yawPID.Ki = GyroKi;
        yawPID.Kd = GyroKd;

        pitchPID.Kp = GyroKp;
        pitchPID.Ki = GyroKi;
        pitchPID.Kd = GyroKd;

        var shipControl = (ShipControlCommons)commons;

        LocalForward = shipControl.ShipBlockOrientation.TransformDirection(ThrusterDirection);
        // Don't really care about "up," just pick a perpindicular direction
        LocalUp = Base6Directions.GetPerpendicular(LocalForward);
        LocalLeft = Base6Directions.GetLeft(LocalUp, LocalForward);

        // Need our own GyroControl instance with its own orientation
        var gyroControl = GetGyroControl(commons);

        gyroControl.Reset();
        gyroControl.EnableOverride(true);

        velocimeter.Reset();
        SampleCount = 60;
        Enabled = true;

        shipControl.ThrustControl.Enable(false);

        MaxGyroError = shipControl.Reference.CubeGrid.GridSize == 0.5f ? 0.0000001 : 0.0001;

        eventDriver.Schedule(0, DetermineVelocity);
    }

    public void DetermineVelocity(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;
        var gyroControl = GetGyroControl(commons);

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        SampleCount--;
        if (SampleCount > 0)
        {
            eventDriver.Schedule(FramesPerRun, DetermineVelocity);
        }
        else
        {
            TargetVector = -((Vector3D)velocimeter.GetAverageVelocity());
            var speed = TargetVector.Normalize();
            if (speed > 0.05)
            {
                eventDriver.Schedule(FramesPerRun, Reorient);
            }
            else
            {
                gyroControl.Reset();
                gyroControl.EnableOverride(false);
                shipControl.ThrustControl.Enable(true);
            }
        }
    }

    public void Reorient(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;
        var gyroControl = GetGyroControl(commons);

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Get reference vectors. Can't use global ones since we want our
        // "forward" to point toward ThrusterDirection
        var referenceForward = GetReferenceVector(shipControl, LocalForward);
        var referenceLeft = GetReferenceVector(shipControl, LocalLeft);
        var referenceUp = GetReferenceVector(shipControl, LocalUp);

        // Determine projection of TargetVector onto our reference unit vectors
        var dotZ = TargetVector.Dot(referenceForward);
        var dotX = TargetVector.Dot(referenceLeft);
        var dotY = TargetVector.Dot(referenceUp);

        var projZ = dotZ * referenceForward;
        var projX = dotX * referenceLeft;
        var projY = dotY * referenceUp;

        // Determine yaw/pitch error by calculating angle between our forward
        // vector and TargetVector
        var yawError = Math.Atan(projX.Length() / projZ.Length());
        var pitchError = Math.Atan(projY.Length() / projZ.Length());

        if (dotZ < 0.0)
        {
            // Actually behind us
            yawError += Math.Sign(yawError) * Math.PI;
        }

        // Set sign according to sign of original dot product
        yawError *= Math.Sign(dotX);
        pitchError *= Math.Sign(-dotY); // NB flipped

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        if ((pitchError * pitchError + yawError * yawError) < MaxGyroError)
        {
            gyroControl.Reset();
            shipControl.ThrustControl.Enable(true);
            eventDriver.Schedule(FramesPerRun, Stop);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Reorient);
        }
    }

    public void Stop(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;
        var gyroControl = GetGyroControl(commons);

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);
        var velocity = velocimeter.GetAverageVelocity();
        var speed = ((Vector3D)velocity).Length();
        if (speed <= 0.05)
        {
            gyroControl.EnableOverride(false);
            return;
        }

        eventDriver.Schedule(FramesPerRun, Stop);
    }

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = GetGyroControl(commons);
        gyroControl.Reset();
        gyroControl.EnableOverride(false);
        shipControl.ThrustControl.Enable(true);
        Enabled = false;
    }

    private GyroControl GetGyroControl(ZACommons commons)
    {
        var gyroControl = new GyroControl();
        gyroControl.Init(commons.Blocks,
                         shipUp: LocalUp,
                         shipForward: LocalForward);
        return gyroControl;
    }

    private Vector3D GetReferenceVector(ShipControlCommons shipControl,
                                        Base6Directions.Direction direction)
    {
        var offset = shipControl.Reference.Position + Base6Directions.GetIntVector(direction);
        return Vector3D.Normalize(shipControl.Reference.CubeGrid.GridIntegerToWorld(offset) - shipControl.ReferencePoint);
    }
}
