//@ shipcontrol pid
public class Seeker
{
    private const double AngleKp = 5.0;
    private const double AngleTi = 0.0;
    private const double AngleTd = 0.08;
    private const double VelKp = 1.0;
    private const double VelTi = 0.0;
    private const double VelTd = 0.08;
    private readonly PIDController yawPID, pitchPID, rollPID;
    private readonly PIDController yawVPID, pitchVPID, rollVPID;

    private Base6Directions.Direction ShipForward, ShipUp, ShipLeft;

    public double ControlThreshold { get; set; }

    public Seeker(double dt)
    {
        yawPID = new PIDController(dt);
        pitchPID = new PIDController(dt);
        rollPID = new PIDController(dt);
        yawVPID = new PIDController(dt);
        pitchVPID = new PIDController(dt);
        rollVPID = new PIDController(dt);

        ControlThreshold = 0.01;
    }

    public void Init(ShipControlCommons shipControl,
                     Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        ShipForward = shipForward;
        ShipUp = shipUp;
        ShipLeft = Base6Directions.GetLeft(ShipUp, ShipForward);

        yawPID.Kp = AngleKp;
        yawPID.Ti = AngleTi;
        yawPID.Td = AngleTd;
        yawPID.min = -Math.PI;
        yawPID.max = Math.PI;

        pitchPID.Kp = AngleKp;
        pitchPID.Ti = AngleTi;
        pitchPID.Td = AngleTd;
        pitchPID.min = -Math.PI;
        pitchPID.max = Math.PI;

        rollPID.Kp = AngleKp / 2.0; // Don't ask
        rollPID.Ti = AngleTi;
        rollPID.Td = AngleTd;
        rollPID.min = -Math.PI;
        rollPID.max = Math.PI;

        yawVPID.Kp = VelKp;
        yawVPID.Ti = VelTi;
        yawVPID.Td = VelTd;
        yawVPID.min = -Math.PI;
        yawVPID.max = Math.PI;

        pitchVPID.Kp = VelKp;
        pitchVPID.Ti = VelTi;
        pitchVPID.Td = VelTd;
        pitchVPID.min = -Math.PI;
        pitchVPID.max = Math.PI;

        rollVPID.Kp = VelKp / 2.0; // Don't ask
        rollVPID.Ti = VelTi;
        rollVPID.Td = VelTd;
        rollVPID.min = -Math.PI;
        rollVPID.max = Math.PI;

        yawPID.Reset();
        pitchPID.Reset();
        rollPID.Reset();
        yawVPID.Reset();
        pitchVPID.Reset();
        rollVPID.Reset();
    }

    // Yaw/pitch only
    public GyroControl Seek(ShipControlCommons shipControl,
                            Vector3D targetVector,
                            out double yawPitchError)
    {
        double rollError;
        return _Seek(shipControl, targetVector, null,
                     out yawPitchError, out rollError);
    }

    // Yaw/pitch/roll
    public GyroControl Seek(ShipControlCommons shipControl,
                            Vector3D targetVector, Vector3D targetUp,
                            out double yawPitchError, out double rollError)
    {
        return _Seek(shipControl, targetVector, targetUp,
                     out yawPitchError, out rollError);
    }

    private GyroControl _Seek(ShipControlCommons shipControl,
                              Vector3D targetVector, Vector3D? targetUp,
                              out double yawPitchError, out double rollError)
    {
        var angularVelocity = shipControl.AngularVelocity;
        if (angularVelocity == null)
        {
            // No ship controller, no action
            yawPitchError = rollError = Math.PI;
            return shipControl.GyroControl;
        }

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

        targetVector = Vector3D.Normalize(targetVector);

        // Invert our world matrix
        var toLocal = MatrixD.Invert(MatrixD.CreateWorld(Vector3D.Zero, referenceForward, referenceUp));

        // And bring targetVector & angular velocity into local space
        var localTarget = Vector3D.Transform(-targetVector, toLocal);
        var localVel = Vector3D.Transform((Vector3D)angularVelocity, toLocal);

        // Use simple trig to get the error angles
        var yawError = Math.Atan2(localTarget.X, localTarget.Z);
        var pitchError = Math.Atan2(localTarget.Y, localTarget.Z);

        // Set desired angular velocity
        var desiredYawVel = yawPID.Compute(yawError);
        var desiredPitchVel = pitchPID.Compute(pitchError);

        //shipControl.Echo(string.Format("desiredVel = {0:F3}, {1:F3}", desiredYawVel, desiredPitchVel));

        // Translate to gyro outputs
        double gyroYaw = 0.0;
        if (Math.Abs(desiredYawVel) >= ControlThreshold)
        {
            gyroYaw = yawVPID.Compute(desiredYawVel - localVel.X);
        }
        double gyroPitch = 0.0;
        if (Math.Abs(desiredPitchVel) >= ControlThreshold)
        {
            gyroPitch = pitchVPID.Compute(desiredPitchVel - localVel.Y);
        }

        //shipControl.Echo(string.Format("yaw, pitch = {0:F3}, {1:F3}", gyroYaw, gyroPitch));

        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        // Determine total yaw/pitch error
        yawPitchError = Math.Acos(MathHelperD.Clamp(Vector3D.Dot(targetVector, referenceForward), -1.0, 1.0));

        if (targetUp != null)
        {
            // Also adjust roll by rotating targetUp vector
            localTarget = Vector3D.Transform((Vector3D)targetUp, toLocal);

            rollError = Math.Atan2(localTarget.X, localTarget.Y);

            var desiredRollVel = rollPID.Compute(rollError);

            //shipControl.Echo(string.Format("desiredRollVel = {0:F3}", desiredRollVel));

            double gyroRoll = 0.0;
            if (Math.Abs(desiredRollVel) >= ControlThreshold)
            {
                gyroRoll = rollVPID.Compute(desiredRollVel - localVel.Z);
            }

            //shipControl.Echo(string.Format("roll = {0:F3}", gyroRoll));

            gyroControl.SetAxisVelocity(GyroControl.Roll, (float)gyroRoll);

            // Only care about absolute error
            rollError = Math.Abs(rollError);
        }
        else
        {
            rollError = 0.0;
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
