//@ shipcontrol eventdriver basemissileguidance seeker quadraticsolver
public class MissileGuidance : BaseMissileGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

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

        InitCamera(commons, eventDriver);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        Vector3D? velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Interpolate position since last update
            var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
            var targetGuess = TargetAimPoint + TargetVelocity * delta.TotalSeconds;

            // Solve for intersection of expanding sphere + moving object
            var offset = targetGuess - shipControl.ReferencePoint;
            var a = Vector3D.Dot(TargetVelocity, TargetVelocity) - Vector3D.Dot((Vector3D)velocity, (Vector3D)velocity);
            var b = 2.0 * Vector3D.Dot(offset, TargetVelocity);
            var c = Vector3D.Dot(offset, offset);

            double interceptTime = 20.0;

            double s1, s2;
            int solutions = QuadraticSolver.Solve(a, b, c, out s1, out s2);
            // Pick smallest positive intercept time
            if (solutions == 1)
            {
                if (s1 > 0.0) interceptTime = s1;
            }
            else if (solutions == 2)
            {
                if (s1 > 0.0) interceptTime = s1;
                else if (s2 > 0.0) interceptTime = s2;
            }

            var prediction = targetGuess + TargetVelocity * interceptTime;

            // Determine relative vector to aim point
            var targetVector = prediction - shipControl.ReferencePoint;
            // Project onto our velocity
            var velocityNorm = Vector3D.Normalize((Vector3D)velocity);
            var forwardProj = velocityNorm * Vector3D.Dot(targetVector, velocityNorm);
            // Use scaled rejection for oversteer
            var forwardRej = (targetVector - forwardProj) * OVERSTEER_FACTOR;
            // Add to projection to get adjusted aimpoint
            var aimPoint = forwardProj + forwardRej;

            double yawPitchError;
            seeker.Seek(shipControl, aimPoint, out yawPitchError);
        }
        else
        {
            // Can't really do crap w/o our own velocity
            shipControl.GyroControl.Reset();
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
