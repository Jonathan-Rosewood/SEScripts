//@ shipcontrol eventdriver basemissileguidance seeker
public class MissileGuidance : BaseMissileGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Vector3D Prediction;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        Prediction = Target;

        eventDriver.Schedule(0, Run);
        eventDriver.Schedule(FULL_BURN_DELAY, FullBurn);
    }

    public void FullBurn(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Full burn
        var thrustControl = shipControl.ThrustControl;
        thrustControl.SetOverride(Base6Directions.Direction.Forward, true);
        // And disable thrusters in all other directions
        thrustControl.Enable(Base6Directions.Direction.Backward, false);
        thrustControl.Enable(Base6Directions.Direction.Up, false);
        thrustControl.Enable(Base6Directions.Direction.Down, false);
        thrustControl.Enable(Base6Directions.Direction.Left, false);
        thrustControl.Enable(Base6Directions.Direction.Right, false);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        Vector3D? velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Interpolate position since last update
            var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
            var targetGuess = Target + TargetVelocity * delta.TotalSeconds;

            var missileSpeed = ((Vector3D)velocity).Length();

            // Get relative positions & distances
            var relativePosition = targetGuess - shipControl.ReferencePoint;
            var targetDistance = relativePosition.Length();
            var predRelativePosition = Prediction - shipControl.ReferencePoint;
            var predDistance = predRelativePosition.Length();
            // Detetermine closing speed by projection
            var closingSpeed = Vector3D.Dot((Vector3D)velocity, predRelativePosition / predDistance);
            // Clamp to lower bound 1/3rd of current speed
            closingSpeed = Math.Max(closingSpeed, missileSpeed / 3.0);
            // Determine time to target using shortest distance
            var timeToTarget = Math.Min(targetDistance, predDistance) / closingSpeed;
            // Aim at point where the target will be after timeToTarget secs
            Prediction = targetGuess + TargetVelocity * timeToTarget;

            // Determine relative vector to aim point
            var targetVector = Prediction - shipControl.ReferencePoint;
            // Project onto our velocity
            var velocityNorm = (Vector3D)velocity / missileSpeed;
            var forwardProj = velocityNorm * Vector3D.Dot(targetVector, velocityNorm);
            // Use scaled rejection for oversteer
            var forwardRej = (targetVector - forwardProj) * OVERSTEER_FACTOR;
            // Add to projection to get adjusted aimpoint
            var aimPoint = forwardProj + forwardRej;

            double yawError, pitchError;
            seeker.Seek(shipControl, aimPoint, out yawError, out pitchError);
        }
        else
        {
            // Can't really do crap w/o our own velocity
            shipControl.GyroControl.Reset();
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
