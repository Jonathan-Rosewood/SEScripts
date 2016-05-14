//@ shipcontrol eventdriver seeker cruiser
public class LOSGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.005);

    private Vector3D LauncherReferencePoint;
    private Vector3D LauncherReferenceDirection;

    private const double LOS_OFFSET = 200.0; // Meters forward
    private const double MANEUVERING_SPEED = 20.0; // In meters per second
    private const double FULL_BURN_DISTANCE = 10.0; // Meters
    private const double FULL_BURN_SPEED = 100.0; // In meters per second
    private readonly TimeSpan FULL_BURN_TRIGGER_TIME = TimeSpan.FromSeconds(3);
    private bool FullBurn = false;
    private TimeSpan FullBurnTriggerLast;

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
        var shipControl = (ShipControlCommons)commons;

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);
        cruiser.Init(shipControl);

        FullBurnTriggerLast = eventDriver.TimeSinceStart;

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

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        if (!FullBurn)
        {
            if (launcherRej.Length() <= FULL_BURN_DISTANCE)
            {
                var triggerTime = eventDriver.TimeSinceStart - FULL_BURN_TRIGGER_TIME;
                if (FullBurnTriggerLast <= triggerTime)
                {
                    FullBurn = true;

                    cruiser.Init(shipControl);
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
                FullBurnTriggerLast = eventDriver.TimeSinceStart;

                // Maintain maneuvering velocity
                Maneuver(commons, eventDriver);
            }
        }

        if (FullBurn) Burn(commons, eventDriver);

        eventDriver.Schedule(FramesPerRun, Run);
    }

    private void Maneuver(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        cruiser.Cruise(shipControl, eventDriver, MANEUVERING_SPEED,
                       enableBackward: false);
    }

    private void Burn(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        cruiser.Cruise(shipControl, eventDriver, FULL_BURN_SPEED,
                       enableBackward: false);
    }
}
