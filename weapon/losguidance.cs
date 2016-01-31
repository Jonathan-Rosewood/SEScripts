public class LOSGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private const double GyroKp = 250.0; // Proportional constant
    private const double GyroKi = 0.0; // Integral constant
    private const double GyroKd = 200.0; // Derivative constant
    private readonly PIDController yawPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController pitchPID = new PIDController(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private Vector3D LauncherReferencePoint;
    private Vector3D LauncherReferenceDirection;

    private const double LOS_OFFSET = 200.0; // Meters forward
    private const double MANEUVERING_SPEED = 20.0; // In meters per second
    private const double FULL_BURN_DISTANCE = 10.0; // Meters
    private readonly TimeSpan FULL_BURN_TRIGGER_TIME = TimeSpan.FromSeconds(3);
    private bool FullBurn = false;
    private DateTime FullBurnTriggerLast;

    public void SetLauncherReference(IMyCubeBlock launcherReference,
                                     Base6Directions.Direction direction = Base6Directions.Direction.Forward)
    {
        LauncherReferencePoint = launcherReference.GetPosition();
        var forward3I = launcherReference.Position + Base6Directions.GetIntVector(launcherReference.Orientation.TransformDirection(direction));
        var forwardPoint = launcherReference.CubeGrid.GridIntegerToWorld(forward3I);
        LauncherReferenceDirection = Vector3D.Normalize(forwardPoint - LauncherReferencePoint);
    }

    public IMyTerminalBlock SetLauncherReference(ZACommons commons, string groupName,
                                                 Base6Directions.Direction direction = Base6Directions.Direction.Forward,
                                                 Func<IMyTerminalBlock, bool> condition = null)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group != null)
        {
            for (var e = group.Blocks.GetEnumerator(); e.MoveNext();)
            {
                var block = e.Current;
                if (condition == null || condition(block))
                {
                    // Use first block that matches condition
                    SetLauncherReference(block, direction);
                    return block;
                }
            }
        }
        throw new Exception("Cannot set launcher reference from group: " + groupName);
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
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

        FullBurnTriggerLast = commons.Now;

        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Vector from launcher to missile
        var launcherVector = shipControl.ReferencePoint - LauncherReferencePoint;
        // Determine projection on launcher direction vector
        var launcherDot = launcherVector.Dot(LauncherReferenceDirection);
        var launcherProj = launcherDot * LauncherReferenceDirection;
        // Also shortest distance from launcher direction vector (rejection)
        var launcherRej = launcherVector - launcherProj;

        Vector3D targetVector;
        if (launcherDot >= 0.0)
        {
            // Set targetVector to that projection
            targetVector = launcherProj;
        }
        else
        {
            // Or set it to the launcher (0,0,0) if the missile is
            // behind the launcher
            targetVector = new Vector3D(0, 0, 0);
        }

        // Offset forward by some amount
        targetVector += LOS_OFFSET * LauncherReferenceDirection;

        // Offset by difference between launcher and missile positions
        targetVector += LauncherReferencePoint - shipControl.ReferencePoint;

        // Determine projection of targetVector onto our reference unit vectors
        var dotZ = targetVector.Dot(shipControl.ReferenceForward);
        var dotX = targetVector.Dot(shipControl.ReferenceLeft);
        var dotY = targetVector.Dot(shipControl.ReferenceUp);

        var projZ = dotZ * shipControl.ReferenceForward;
        var projX = dotX * shipControl.ReferenceLeft;
        var projY = dotY * shipControl.ReferenceUp;

        // Determine yaw/pitch error by calculating angle between our forward
        // vector and targetVector
        var yawError = Math.Atan(projX.Length() / projZ.Length());
        var pitchError = Math.Atan(projY.Length() / projZ.Length());

        if (dotZ < 0.0)
        {
            // Actually behind us
            yawError += Math.Sign(yawError) * Math.PI;
        }

        // Set sign according to sign of original dot product
        yawError *= Math.Sign(dotX);
        pitchError *= Math.Sign(-dotY); // NB flipped

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        var gyroControl = shipControl.GyroControl;
        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        if (!FullBurn)
        {
            if (launcherRej.Length() <= FULL_BURN_DISTANCE)
            {
                var triggerTime = commons.Now - FULL_BURN_TRIGGER_TIME;
                if (FullBurnTriggerLast <= triggerTime)
                {
                    FullBurn = true;

                    // Max forward thrust
                    shipControl.ThrustControl.SetOverride(Base6Directions.Direction.Forward);
                }
                else
                {
                    // Maintain maneuvering velocity
                    Maneuver(commons, eventDriver);
                }
            }
            else
            {
                // Reset trigger timer
                FullBurnTriggerLast = commons.Now;

                // Maintain maneuvering velocity
                Maneuver(commons, eventDriver);
            }
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }

    private void Maneuver(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            // Only absolute velocity
            var speed = ((Vector3D)velocity).Length();
            var error = MANEUVERING_SPEED - speed;

            var force = thrustPID.Compute(error);

            var thrustControl = shipControl.ThrustControl;
            if (force > 0.0)
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, force);
            }
            else
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, false);
            }
        }
    }
}
