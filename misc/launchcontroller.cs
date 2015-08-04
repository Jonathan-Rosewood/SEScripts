public class LaunchController
{
    private const string BATTERY_GROUP = "Relay Batteries";
    private const string SYSTEMS_GROUP = "Relay Systems";
    private const string RELEASE_GROUP = "Relay Release";
    private const string REMOTE_GROUP = "Relay RC";

    public Base6Directions.Direction ShipUp { get; private set; }
    public Base6Directions.Direction ShipForward { get; private set; }

    private bool IsInLauncher(ZACommons commons)
    {
        var launcherRelease = commons.GetBlockGroupWithName("Launcher Release");
        return launcherRelease != null;
    }

    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remoteGroup = commons.GetBlockGroupWithName(REMOTE_GROUP);
        if (remoteGroup == null)
        {
            throw new Exception("Missing group: " + REMOTE_GROUP);
        }
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(remoteGroup.Blocks);
        if (remotes.Count != 1)
        {
            throw new Exception("Expecting exactly 1 remote control");
        }
        return remotes[0];
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(commons);
        ShipUp = remote.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = remote.Orientation.TransformDirection(Base6Directions.Direction.Forward);

        // Determine current state
        if (IsInLauncher(commons))
        {
            eventDriver.Schedule(0.0, Prime);
        }
        else if (remote.GetValue<bool>("AutoPilot"))
        {
            eventDriver.Schedule(0.0, AutopilotEnd);
        }
        else
        {
            eventDriver.Schedule(0.0); // Schedule main loop
        }
    }

    public void Prime(ZACommons commons, EventDriver eventDriver)
    {
        var relayBatteries = commons.GetBlockGroupWithName(BATTERY_GROUP);
        if (relayBatteries == null)
        {
            throw new Exception("Missing group: " + BATTERY_GROUP);
        }
        var relaySystems = commons.GetBlockGroupWithName(SYSTEMS_GROUP);
        if (relaySystems == null)
        {
            throw new Exception("Missing group: " + SYSTEMS_GROUP);
        }

        // Wake up batteries
        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(relayBatteries.Blocks);
        ZACommons.EnableBlocks(batteries, true);
        ZACommons.SetBatteryRecharge(batteries, false);
        // And activate flight systems
        ZACommons.EnableBlocks(relaySystems.Blocks, true);

        eventDriver.Schedule(1.0, Release);
    }

    public void Release(ZACommons commons, EventDriver eventDriver)
    {
        var relayRelease = commons.GetBlockGroupWithName(RELEASE_GROUP);
        if (relayRelease == null)
        {
            throw new Exception("Missing group: " + RELEASE_GROUP);
        }
        ZACommons.EnableBlocks(relayRelease.Blocks, false);

        eventDriver.Schedule(1.0, Burn);
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        var thrustControl = new ThrustControl();
        thrustControl.Init(commons.Blocks, shipUp: ShipUp, shipForward: ShipForward);
        thrustControl.SetOverride(Base6Directions.Direction.Down);

        eventDriver.Schedule(LAUNCH_BURN_DURATION, BurnEnd);
    }

    public void BurnEnd(ZACommons commons, EventDriver eventDriver)
    {
        var thrustControl = new ThrustControl();
        thrustControl.Init(commons.Blocks, shipUp: ShipUp, shipForward: ShipForward);
        thrustControl.Reset();

        eventDriver.Schedule(0.0, Autopilot);
    }

    public void Autopilot(ZACommons commons, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(commons);
        remote.SetValue<bool>("AutoPilot", true);

        eventDriver.Schedule(1.0, AutopilotEnd);
    }

    public void AutopilotEnd(ZACommons commons, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(commons);
        if (remote.GetValue<bool>("AutoPilot"))
        {
            // Wait it out
            eventDriver.Schedule(1.0, AutopilotEnd);
        }
        else
        {
            // All done, schedule main loop
            eventDriver.Schedule(0.0);
        }
    }
}
