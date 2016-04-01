//@ shipcontrol eventdriver seeker velocimeter
public class ReverseThrust
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private int SampleCount;

    private Base6Directions.Direction ThrusterDirection;
    private bool Enabled;
    private Vector3D TargetVector;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Base6Directions.Direction thrusterDirection = Base6Directions.Direction.Forward)
    {
        ThrusterDirection = thrusterDirection;
        
        var shipControl = (ShipControlCommons)commons;

        var forward = shipControl.ShipBlockOrientation.TransformDirection(ThrusterDirection);
        // Don't really care about "up," just pick a perpindicular direction
        seeker.Init(shipControl,
                    shipUp: Base6Directions.GetPerpendicular(forward),
                    shipForward: forward);

        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(true);

        velocimeter.Reset();
        SampleCount = 60;
        Enabled = true;

        shipControl.ThrustControl.Enable(false);

        eventDriver.Schedule(0, DetermineVelocity);
    }

    public void DetermineVelocity(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;

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
            if (speed > 0.1)
            {
                eventDriver.Schedule(FramesPerRun, Reorient);
            }
            else
            {
                var gyroControl = shipControl.GyroControl;
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

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        double yawError, pitchError;
        var gyroControl = seeker.Seek(shipControl, TargetVector,
                                      out yawError, out pitchError);

        if ((pitchError * pitchError + yawError * yawError) < REVERSE_THRUST_MAX_GYRO_ERROR)
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

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);
        var velocity = velocimeter.GetAverageVelocity();
        var speed = ((Vector3D)velocity).Length();
        if (speed <= 0.1)
        {
            shipControl.GyroControl.EnableOverride(false);
            return;
        }

        eventDriver.Schedule(FramesPerRun, Stop);
    }

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(false);
        shipControl.ThrustControl.Enable(true);
        Enabled = false;
    }
}
