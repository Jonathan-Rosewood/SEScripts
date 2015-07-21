public class LaunchController
{
    private const string BATTERY_GROUP = "Relay Batteries";
    private const string SYSTEMS_GROUP = "Relay Systems";
    private const string RELEASE_GROUP = "Relay Release";
    private const string REMOTE_GROUP = "Relay RC";

    public Base6Directions.Direction ShipUp { get; private set; }
    public Base6Directions.Direction ShipForward { get; private set; }

    private bool IsInLauncher(MyGridProgram program)
    {
        var launcherRelease = ZALibrary.GetBlockGroupWithName(program, "Launcher Release");
        return launcherRelease != null;
    }

    private IMyRemoteControl GetRemoteControl(MyGridProgram program)
    {
        var remoteGroup = ZALibrary.GetBlockGroupWithName(program, REMOTE_GROUP);
        if (remoteGroup == null)
        {
            throw new Exception("Missing group: " + REMOTE_GROUP);
        }
        var remotes = ZALibrary.GetBlocksOfType<IMyRemoteControl>(remoteGroup.Blocks);
        if (remotes.Count != 1)
        {
            throw new Exception("Expecting exactly 1 remote control");
        }
        return remotes[0];
    }

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(program);
        ShipUp = remote.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = remote.Orientation.TransformDirection(Base6Directions.Direction.Forward);

        // Determine current state
        if (IsInLauncher(program))
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

    public void Prime(MyGridProgram program, EventDriver eventDriver)
    {
        var relayBatteries = ZALibrary.GetBlockGroupWithName(program, BATTERY_GROUP);
        if (relayBatteries == null)
        {
            throw new Exception("Missing group: " + BATTERY_GROUP);
        }
        var relaySystems = ZALibrary.GetBlockGroupWithName(program, SYSTEMS_GROUP);
        if (relaySystems == null)
        {
            throw new Exception("Missing group: " + SYSTEMS_GROUP);
        }

        // Wake up batteries
        var batteries = ZALibrary.GetBlocksOfType<IMyBatteryBlock>(relayBatteries.Blocks);
        ZALibrary.EnableBlocks(batteries, true);
        ZALibrary.SetBatteryRecharge(batteries, false);
        // And activate flight systems
        ZALibrary.EnableBlocks(relaySystems.Blocks, true);

        eventDriver.Schedule(2.0, Release);
    }

    public void Release(MyGridProgram program, EventDriver eventDriver)
    {
        var relayRelease = ZALibrary.GetBlockGroupWithName(program, RELEASE_GROUP);
        if (relayRelease == null)
        {
            throw new Exception("Missing group: " + RELEASE_GROUP);
        }
        ZALibrary.EnableBlocks(relayRelease.Blocks, false);

        eventDriver.Schedule(0.5, Burn);
    }

    public void Burn(MyGridProgram program, EventDriver eventDriver)
    {
        var thrustControl = new ThrustControl();
        thrustControl.Init(program, shipUp: ShipUp, shipForward: ShipForward);
        // NB We inherit the launcher's grid pivot
        thrustControl.SetOverride(Base6Directions.Direction.Down);

        eventDriver.Schedule(LAUNCH_BURN_DURATION, BurnEnd);
    }

    public void BurnEnd(MyGridProgram program, EventDriver eventDriver)
    {
        var thrustControl = new ThrustControl();
        thrustControl.Init(program, shipUp: ShipUp, shipForward: ShipForward);
        thrustControl.Reset();

        eventDriver.Schedule(0.0, Autopilot);
    }

    public void Autopilot(MyGridProgram program, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(program);
        remote.SetValue<bool>("AutoPilot", true);

        eventDriver.Schedule(1.0, AutopilotEnd);
    }

    public void AutopilotEnd(MyGridProgram program, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(program);
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
