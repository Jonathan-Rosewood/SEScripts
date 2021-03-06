//@ shipcontrol eventdriver basemissileguidance seeker
public class ProNavGuidance : BaseMissileGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private double ForwardAcceleration;

    private TimeSpan OneTurnEnd;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        if (shipControl.ShipController == null)
        {
            throw new Exception("No ship controller on board?");
        }

        // Figure out max forward acceleration, a=F/m
        float maxForce = 0.0f;
        var thrusterList = shipControl.ThrustControl.GetThrusters(Base6Directions.Direction.Forward);
        thrusterList.ForEach(thruster => maxForce += thruster.MaxThrust);
        ForwardAcceleration = maxForce / shipControl.ShipController.CalculateShipMass().PhysicalMass;

        OneTurnEnd = eventDriver.TimeSinceStart + TimeSpan.FromSeconds(ONE_TURN_DURATION);

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        eventDriver.Schedule(0, OneTurn);
    }

    public void OneTurn(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Interpolate position since last update
        var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
        var targetGuess = TargetAimPoint + TargetVelocity * delta.TotalSeconds;

        var targetVector = targetGuess - shipControl.ReferencePoint;

        double yawPitchError;
        seeker.Seek(shipControl, targetVector, out yawPitchError);

        if (OneTurnEnd < eventDriver.TimeSinceStart)
        {
            FullBurn(commons, eventDriver);
            eventDriver.Schedule(0, Run);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, OneTurn);
        }
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

            // Do PN
            var offset = targetGuess - shipControl.ReferencePoint;
            var relativeVelocity = TargetVelocity - (Vector3D)velocity;
            var omega = offset.Cross(relativeVelocity) / offset.Dot(offset);
            var direction = Vector3D.Normalize((Vector3D)velocity);
            var accel = Vector3D.Cross(direction * -PN_GUIDANCE_GAIN * relativeVelocity.Length(), omega);

            // Translate acceleration to an aim point
            // Basically, we know 2 sides of a right triangle:
            // ForwardAcceleration (hyp) and accel. The third side is coincident
            // with velocity. This is what we're after (vDistance).
            var vDistance = Math.Sqrt(Math.Max(ForwardAcceleration * ForwardAcceleration -
                                               Vector3D.Dot(accel, accel), 0.0));
            var aimPoint = Vector3D.Normalize(direction * vDistance + accel);

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
