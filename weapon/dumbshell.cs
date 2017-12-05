//@ shipcontrol eventdriver
public class DumbShell
{
    private const string BATTERY_GROUP = "Shell Batteries";
    private const string SYSTEMS_GROUP = "Shell Systems";
    private const string RELEASE_GROUP = "Shell Attach";
    private const string MASS_GROUP = "Shell Mass";

    private const uint TicksPerRun = 1;
    private const double RunsPerSecond = 60.0 / TicksPerRun;

    private Action<ZACommons, EventDriver> PostLaunch;
    public Vector3D InitialPosition { get; private set; }
    public TimeSpan InitialTime { get; private set; }
    public Vector3D LauncherVelocity { get; private set; }

    private TimeSpan AccelStartTime;
    private Vector3D AccelStartPosition, AccelLastPosition;
    private readonly StringBuilder AccelResults = new StringBuilder();

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> postLaunch = null)
    {
        PostLaunch = postLaunch;
        InitialPosition = ((ShipControlCommons)commons).ReferencePoint;
        InitialTime = eventDriver.TimeSinceStart;
        eventDriver.Schedule(0.0, Prime);
    }

    public void Prime(ZACommons commons, EventDriver eventDriver)
    {
        var batteryGroup = commons.GetBlockGroupWithName(BATTERY_GROUP);
        if (batteryGroup == null)
        {
            throw new Exception("Group missing: " + BATTERY_GROUP);
        }
        var systemsGroup = commons.GetBlockGroupWithName(SYSTEMS_GROUP);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP);
        }

        // Wake up batteries
        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(batteryGroup.Blocks);
        batteries.ForEach(battery =>
                {
                    battery.Enabled = true;
                    battery.OnlyDischarge = true;
                });

        // Activate systems
        ZACommons.EnableBlocks(systemsGroup.Blocks, true);

        eventDriver.Schedule(0.1, Release);
    }

    public void Release(ZACommons commons, EventDriver eventDriver)
    {
        // Enable mass
        var group = commons.GetBlockGroupWithName(MASS_GROUP);
        if (group != null) ZACommons.EnableBlocks(group.Blocks, true);

        var releaseGroup = commons.GetBlockGroupWithName(RELEASE_GROUP);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + RELEASE_GROUP);
        }

        // Get one last reading from launcher and determine velocity
        var launcherDelta = ((ShipControlCommons)commons).ReferencePoint -
            InitialPosition;
        var deltaTime = (eventDriver.TimeSinceStart - InitialTime).TotalSeconds;
        LauncherVelocity = launcherDelta / deltaTime;

        // Turn release group off
        ZACommons.EnableBlocks(releaseGroup.Blocks, false);

        // Statistics
        AccelStartTime = eventDriver.TimeSinceStart;
        AccelStartPosition = commons.Me.GetPosition();
        AccelLastPosition = AccelStartPosition;
        AccelResults.Append(string.Format("CoM Distance: {0:F2} m\n", (AccelStartPosition - ((ShipControlCommons)commons).ReferencePoint).Length()));
        eventDriver.Schedule(TicksPerRun, AccelCheck);

        eventDriver.Schedule(1.0, Demass);
    }

    public void AccelCheck(ZACommons commons, EventDriver eventDriver)
    {
        var position = commons.Me.GetPosition();
        // Approximate speed
        var speed = (position - AccelLastPosition).Length() * RunsPerSecond;
        // Do some rounding
        speed = Math.Ceiling(speed * 10.0 + 0.5) / 10.0;
        if (speed < 100.0) // TODO
        {
            AccelLastPosition = position;
            eventDriver.Schedule(TicksPerRun, AccelCheck);
            return;
        }

        // All done
        AccelResults.Append(string.Format("Accel. Time: {0:F3} s\n", (eventDriver.TimeSinceStart - AccelStartTime).TotalSeconds));
        AccelResults.Append(string.Format("Accel. Distance: {0:F2} m\n", (position - AccelStartPosition).Length()));
    }

    public void Demass(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var deltaTime = (eventDriver.TimeSinceStart - InitialTime).TotalSeconds;
        var launcherDelta = LauncherVelocity * deltaTime;
        var distanceFromLauncher = (shipControl.ReferencePoint -
                                    (InitialPosition + launcherDelta)).LengthSquared();

        if (distanceFromLauncher < DemassDistance * DemassDistance)
        {
            // Not yet
            eventDriver.Schedule(TicksPerRun, Demass);
            return;
        }

        // Disable mass
        var group = commons.GetBlockGroupWithName(MASS_GROUP);
        if (group != null)  ZACommons.EnableBlocks(group.Blocks, false);

        // Start roll
        shipControl.GyroControl.EnableOverride(true);
        shipControl.GyroControl.SetAxisVelocity(GyroControl.Roll,
                                                MathHelper.Pi);

        // All done
        if (PostLaunch != null) PostLaunch(commons, eventDriver);
    }

    public void Display(ZACommons commons)
    {
        commons.Echo(AccelResults.ToString());
    }
}
