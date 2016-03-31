public class Seeker
{
    private const double SmallGyroKp = 250.0; // Proportional constant
    private const double SmallGyroKi = 0.0; // Integral constant
    private const double SmallGyroKd = 200.0; // Derivative constant
    private const double LargeGyroKp = 50.0; // Proportional constant
    private const double LargeGyroKi = 0.0; // Integral constant
    private const double LargeGyroKd = 40.0; // Derivative constant
    private readonly PIDController yawPID, pitchPID, rollPID;

    private Base6Directions.Direction ShipForward, ShipUp, ShipLeft;

    public Seeker(double dt)
    {
        yawPID = new PIDController(dt);
        pitchPID = new PIDController(dt);
        rollPID = new PIDController(dt);
    }

    public void Init(ShipControlCommons shipControl,
                     Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        ShipForward = shipForward;
        ShipUp = shipUp;
        ShipLeft = Base6Directions.GetLeft(ShipUp, ShipForward);

        var small = shipControl.Reference.CubeGrid.GridSize == 0.5f;

        yawPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        yawPID.Ki = small ? SmallGyroKi : LargeGyroKi;
        yawPID.Kd = small ? SmallGyroKd : LargeGyroKd;

        pitchPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        pitchPID.Ki = small ? SmallGyroKi : LargeGyroKi;
        pitchPID.Kd = small ? SmallGyroKd : LargeGyroKd;

        rollPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        rollPID.Ki = small ? SmallGyroKi : LargeGyroKi;
        rollPID.Kd = small ? SmallGyroKd : LargeGyroKd;

        yawPID.Reset();
        pitchPID.Reset();
        rollPID.Reset();
    }

    // Yaw/pitch only
    public GyroControl Seek(ShipControlCommons shipControl,
                            Vector3D targetVector,
                            out double yawError, out double pitchError)
    {
        double rollError;
        return _Seek(shipControl, targetVector, null,
                     out yawError, out pitchError, out rollError);
    }

    // Yaw/pitch/roll
    public GyroControl Seek(ShipControlCommons shipControl,
                            Vector3D targetVector, Vector3D targetUp,
                            out double yawError, out double pitchError,
                            out double rollError)
    {
        return _Seek(shipControl, targetVector, targetUp,
                     out yawError, out pitchError, out rollError);
    }

    private GyroControl _Seek(ShipControlCommons shipControl,
                              Vector3D targetVector, Vector3D? targetUp,
                              out double yawError, out double pitchError,
                              out double rollError)
    {
        Vector3D referenceForward;
        Vector3D referenceLeft;
        Vector3D referenceUp;
        GyroControl gyroControl;

        // See if local orientation is the same as the ship
        if (shipControl.ShipUp == ShipUp && shipControl.ShipForward == ShipForward)
        {
            // Use same reference vectors and GyroControl
            referenceForward = shipControl.ReferenceForward;
            referenceLeft = shipControl.ReferenceLeft;
            referenceUp = shipControl.ReferenceUp;
            gyroControl = shipControl.GyroControl;
        }
        else
        {
            referenceForward = GetReferenceVector(shipControl, ShipForward);
            referenceLeft = GetReferenceVector(shipControl, ShipLeft);
            referenceUp = GetReferenceVector(shipControl, ShipUp);
            // Need our own GyroControl instance in this case
            gyroControl = new GyroControl();
            gyroControl.Init(shipControl.Blocks,
                             shipUp: ShipUp,
                             shipForward: ShipForward);
        }

        // Determine projection of targetVector onto our reference unit vectors
        var dotZ = targetVector.Dot(referenceForward);
        var dotX = targetVector.Dot(referenceLeft);
        var dotY = targetVector.Dot(referenceUp);

        var projZ = dotZ * referenceForward;
        var projX = dotX * referenceLeft;
        var projY = dotY * referenceUp;

        // Determine yaw/pitch error by calculating angle between our forward
        // vector and targetVector
        var z = projZ.Length() * Math.Sign(dotZ);
        var x = projX.Length() * Math.Sign(dotX);
        var y = projY.Length() * Math.Sign(-dotY); // NB inverted
        yawError = Math.Atan2(x, z);
        pitchError = Math.Atan2(y, z);

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        if (targetUp != null)
        {
            // Also adjust roll
            dotX = ((Vector3D)targetUp).Dot(referenceLeft);
            dotY = ((Vector3D)targetUp).Dot(referenceUp);

            projX = dotX * referenceLeft;
            projY = dotY * referenceUp;

            x = projX.Length() * Math.Sign(-dotX);
            y = projY.Length() * Math.Sign(dotY);
            rollError = Math.Atan2(x, y);

            var gyroRoll = rollPID.Compute(rollError);

            gyroControl.SetAxisVelocity(GyroControl.Roll, (float)gyroRoll);
        }
        else
        {
            rollError = default(double);
        }

        return gyroControl;
    }

    private Vector3D GetReferenceVector(ShipControlCommons shipControl,
                                        Base6Directions.Direction direction)
    {
        var offset = shipControl.Reference.Position + Base6Directions.GetIntVector(direction);
        return Vector3D.Normalize(shipControl.Reference.CubeGrid.GridIntegerToWorld(offset) - shipControl.ReferencePoint);
    }
}
