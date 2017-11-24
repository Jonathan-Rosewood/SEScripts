//@ shipcontrol eventdriver seeker
public class ReverseThrust
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private const uint SampleDelay = 60;
    private Vector3D LastPosition;

    private double MaxError;
    private Base6Directions.Direction ThrusterDirection;
    private bool Enabled;
    private Vector3D TargetVector;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     double maxError,
                     Base6Directions.Direction thrusterDirection = Base6Directions.Direction.Forward)
    {
        MaxError = maxError;
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

        LastPosition = shipControl.ReferencePoint;

        Enabled = true;

        shipControl.ThrustControl.Enable(false);

        eventDriver.Schedule(SampleDelay, DetermineVelocity);
    }

    public void DetermineVelocity(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;

        var velocity = (shipControl.ReferencePoint - LastPosition) /
            ((double)SampleDelay / 60.0);

        TargetVector = -velocity;
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

    public void Reorient(ZACommons commons, EventDriver eventDriver)
    {
        if (!Enabled) return;

        var shipControl = (ShipControlCommons)commons;

        double yawPitchError;
        var gyroControl = seeker.Seek(shipControl, TargetVector,
                                      out yawPitchError);

        if (yawPitchError < MaxError)
        {
            gyroControl.Reset();
            shipControl.ThrustControl.Enable(true);
            gyroControl.Reset();
            gyroControl.EnableOverride(false);
            // Done
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Reorient);
        }
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
