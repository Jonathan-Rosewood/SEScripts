public class SmartUndock
{
    private IMyCubeBlock AutopilotReference;
    private Vector3D AutopilotTarget;
    private Base6Directions.Direction AutopilotForward, AutopilotUp;
    private double AutopilotSpeed;

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        if (argument == "smartundock")
        {
            // First, determine which connector we were connected through
            IMyShipConnector connected = null;
            var connectors = ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks,
                                                                         connector => connector.DefinitionDisplayNameText == "Connector"); // Avoid Ejectors
            for (var e = connectors.GetEnumerator(); e.MoveNext();)
            {
                var connector = e.Current;
                if (connector.IsLocked && connector.IsConnected)
                {
                    // Assume the first one as well
                    connected = connector;
                    break;
                }
            }

            AutopilotReference = null;
            if (connected != null)
            {
                AutopilotReference = connected; // Hmmm, not sure about this

                // Undock the opposite direction of connector
                AutopilotForward = AutopilotReference.Orientation.TransformDirection(Base6Directions.Direction.Backward);
                AutopilotUp = AutopilotReference.Orientation.TransformDirection(Base6Directions.Direction.Up);

                var backwardPoint = AutopilotReference.CubeGrid.GridIntegerToWorld(AutopilotReference.Position + Base6Directions.GetIntVector(AutopilotForward));
                var backwardVector = Vector3D.Normalize(backwardPoint - AutopilotReference.GetPosition());
                // Determine target undock point
                AutopilotTarget = AutopilotReference.GetPosition() + SMART_UNDOCK_DISTANCE * backwardVector;

                AutopilotSpeed = SMART_UNDOCK_UNDOCK_SPEED;

                // Schedule the autopilot
                eventDriver.Schedule(2.0, AutopilotStart);
            }

            // Next, physically undock
            ZACommons.EnableBlocks(connectors, false);
            // Unlock landing gears as well
            var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
            gears.ForEach(gear =>
                    {
                        if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
                    });
        }
        else if (argument == "rtb")
        {
            // If we don't have an existing reference, we don't have an
            // existing target
            if (AutopilotReference == null) return;

            // Find the ship controller that issued this command
            var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks,
                                                                           controller => controller.IsUnderControl);

            if (controllers.Count > 0)
            {
                // Use ship controller as new reference
                AutopilotReference = controllers[0];
            } // Otherwise use existing reference

            AutopilotForward = AutopilotReference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
            AutopilotUp = AutopilotReference.Orientation.TransformDirection(Base6Directions.Direction.Up);

            AutopilotSpeed = SMART_UNDOCK_RTB_SPEED;

            // Schedule the autopilot
            eventDriver.Schedule(1.0, AutopilotStart);
        }
    }

    // Ripped from my missile guidance script...

    public static Vector3D Zero3D = new Vector3D();
    public static Vector3D Forward3D = new Vector3D(0.0, 0.0, 1.0);

    public struct Orientation
    {
        public Vector3D Point;
        public Vector3D Forward;
        public Vector3D Up;
        public Vector3D Left;

        public Orientation(IMyCubeBlock reference,
                           Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                           Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
        {
            Point = reference.GetPosition();
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipForward);
            Forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - Point);
            var up3I = reference.Position + Base6Directions.GetIntVector(shipUp);
            Up = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(up3I) - Point);
            var left3I = reference.Position + Base6Directions.GetIntVector(Base6Directions.GetLeft(shipUp, shipForward));
            Left = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(left3I) - Point);
        }
    }

    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private const double GyroMaxRadiansPerSecond = Math.PI; // Really pi*2, but something's odd...

    private const double GyroKp = 1.0; // Proportional constant
    private const double GyroKi = 0.0; // Integral constant
    private const double GyroKd = 0.0; // Derivative constant
    private readonly PIDController yawPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController pitchPID = new PIDController(1.0 / RunsPerSecond);

    // From my miner script...

    private readonly Velocimeter velocimeter = new Velocimeter(10);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 10000.0;
    private const double ThrustKi = 100.0;
    private const double ThrustKd = 0.0;

    public SmartUndock()
    {
        yawPID.Kp = GyroKp;
        yawPID.Ki = GyroKi;
        yawPID.Kd = GyroKd;

        pitchPID.Kp = GyroKp;
        pitchPID.Ki = GyroKi;
        pitchPID.Kd = GyroKd;

        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    public void AutopilotStart(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Reset all flight systems
        shipControl.GyroControl.Reset();
        shipControl.GyroControl.EnableOverride(true);
        shipControl.ThrustControl.Reset();
        shipControl.ThrustControl.SetOverride(AutopilotForward); // Get moving
        yawPID.Reset();
        pitchPID.Reset();
        thrustPID.Reset();
        velocimeter.Reset();

        eventDriver.Schedule(0, Run); // We real-time now
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var orientation = new Orientation(AutopilotReference,
                                          shipUp: AutopilotUp,
                                          shipForward: AutopilotForward);

        var targetVector = AutopilotTarget - orientation.Point;
        var distance = targetVector.Normalize();

        // Transform relative to our forward vector
        targetVector = Vector3D.Transform(targetVector, MatrixD.CreateLookAt(Zero3D, -orientation.Forward, orientation.Up));

        var yawVector = new Vector3D(targetVector.GetDim(0), 0, targetVector.GetDim(2));
        var pitchVector = new Vector3D(0, targetVector.GetDim(1), targetVector.GetDim(2));
        yawVector.Normalize();
        pitchVector.Normalize();

        var yawError = Math.Acos(Vector3D.Dot(yawVector, Forward3D)) * Math.Sign(targetVector.GetDim(0));
        var pitchError = -Math.Acos(Vector3D.Dot(pitchVector, Forward3D)) * Math.Sign(targetVector.GetDim(1));

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        var gyroControl = shipControl.GyroControl;
        var thrustControl = shipControl.ThrustControl;

        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        // Velocity control
        velocimeter.TakeSample(AutopilotReference.GetPosition(), eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            var speed = Vector3D.Dot((Vector3D)velocity, orientation.Forward);

            var timeToTarget = distance / speed;
            double targetSpeed;
            if (timeToTarget < SMART_UNDOCK_TTT_BUFFER)
            {
                targetSpeed = SMART_UNDOCK_MIN_SPEED;
            }
            else
            {
                targetSpeed = Math.Min(distance / SMART_UNDOCK_TTT_BUFFER,
                                       AutopilotSpeed);
                targetSpeed = Math.Max(targetSpeed, SMART_UNDOCK_MIN_SPEED); // Avoid Zeno's paradox...
            }

            var error = targetSpeed - speed;

            var force = thrustPID.Compute(error);

            var backward = Base6Directions.GetFlippedDirection(AutopilotForward);

            if (force > 0.0)
            {
                // Thrust forward
                thrustControl.SetOverride(AutopilotForward, (float)force);
                thrustControl.SetOverride(backward, 0.0f);
            }
            else
            {
                thrustControl.SetOverride(AutopilotForward, 0.0f);
                thrustControl.SetOverride(backward, (float)(-force));
            }
        }

        if (distance < 1.0)
        {
            // All done
            gyroControl.Reset();
            gyroControl.EnableOverride(false);
            thrustControl.Reset();
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }
}
