//@ shipcontrol eventdriver basemissileguidance
public class ProNavGuidance : BaseMissileGuidance
{
    private const uint FramesPerRun = 1;

    public void Init(ZACommons commons, EventDriver eventDriver)
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

        // Reset gyro
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();

        eventDriver.Schedule(0, Run);
    }

    private float ClampRPM(float value)
    {
        return Math.Max(-PN_MAX_GYRO_RPM, Math.Min(PN_MAX_GYRO_RPM, value));
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = shipControl.GyroControl;

        Vector3D? velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Interpolate position since last update
            var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
            var TargetGuess = Target + TargetVelocity * delta.TotalSeconds;

            // Do PN
            var offset = TargetGuess - shipControl.ReferencePoint;
            var relativeVelocity = TargetVelocity - (Vector3D)velocity;
            var omega = offset.Cross(relativeVelocity) / offset.Dot(offset);
            var direction = Vector3D.Normalize((Vector3D)velocity);
            var accel = Vector3D.Cross(direction * -PN_GUIDANCE_GAIN * relativeVelocity.Length(), omega);

            // Translate into local space
            var toLocal = MatrixD.Invert(MatrixD.CreateWorld(Vector3D.Zero, shipControl.ReferenceForward, shipControl.ReferenceUp));
            var localAccel = Vector3D.Transform(-accel, toLocal);

            var yaw = ClampRPM((float)localAccel.X);
            var pitch = ClampRPM((float)localAccel.Y);

            // Actuate the gyros
            gyroControl.SetAxisVelocityRPM(GyroControl.Yaw, yaw);
            gyroControl.SetAxisVelocityRPM(GyroControl.Pitch, pitch);
        }
        else
        {
            // Can't really do crap w/o our own velocity
            gyroControl.Reset();
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
