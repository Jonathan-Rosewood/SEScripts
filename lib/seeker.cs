//@ shipcontrol pid
public class Seeker
{
    private const double SmallGyroKp = 100.0; // Proportional constant
    private const double SmallGyroTi = 5.0; // Integral constant
    private const double SmallGyroTd = 0.3; // Derivative constant
    private const double LargeGyroKp = 100.0; // Proportional constant
    private const double LargeGyroTi = 20.0; // Integral constant
    private const double LargeGyroTd = 1.0; // Derivative constant
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

        var small = shipControl.Me.CubeGrid.GridSize == 0.5f;

        yawPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        yawPID.Ti = small ? SmallGyroTi : LargeGyroTi;
        yawPID.Td = small ? SmallGyroTd : LargeGyroTd;

        pitchPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        pitchPID.Ti = small ? SmallGyroTi : LargeGyroTi;
        pitchPID.Td = small ? SmallGyroTd : LargeGyroTd;

        rollPID.Kp = small ? SmallGyroKp : LargeGyroKp;
        rollPID.Ti = small ? SmallGyroTi : LargeGyroTi;
        rollPID.Td = small ? SmallGyroTd : LargeGyroTd;

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

        // Invert our world matrix
        var toLocal = MatrixD.Invert(MatrixD.CreateWorld(Vector3D.Zero, referenceForward, referenceUp));

        // And bring targetVector into local space
        var localTarget = Vector3D.Transform(-targetVector, toLocal);

        // Finally use simple trig to get the error angles
        yawError = Math.Atan2(localTarget.X, localTarget.Z);
        pitchError = Math.Atan2(localTarget.Y, localTarget.Z);

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        gyroControl.SetAxisVelocityFraction(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocityFraction(GyroControl.Pitch, (float)gyroPitch);

        if (targetUp != null)
        {
            // Also adjust roll by rotating targetUp vector
            localTarget = Vector3D.Transform((Vector3D)targetUp, toLocal);

            rollError = Math.Atan2(localTarget.X, localTarget.Y);

            var gyroRoll = rollPID.Compute(rollError);

            gyroControl.SetAxisVelocityFraction(GyroControl.Roll, (float)gyroRoll);
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
        var offset = shipControl.Me.Position + Base6Directions.GetIntVector(direction);
        return Vector3D.Normalize(shipControl.Me.CubeGrid.GridIntegerToWorld(offset) - shipControl.Me.GetPosition());
    }
}
