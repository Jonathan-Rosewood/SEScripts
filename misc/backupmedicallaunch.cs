public class BackupMedicalLaunch
{
    public Base6Directions.Direction ShipUp { get; private set; }
    public Base6Directions.Direction ShipForward { get; private set; }

    private IMyRemoteControl GetRemoteControl(MyGridProgram program)
    {
        var remotes = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remotes);
        if (remotes.Count != 1)
        {
            throw new Exception("Expecting exactly 1 remote control");
        }
        return (IMyRemoteControl)remotes[0];
    }

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(program);
        ShipUp = remote.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = remote.Orientation.TransformDirection(Base6Directions.Direction.Forward);

        // Determine current state
        if (remote.GetValue<bool>("AutoPilot"))
        {
            eventDriver.Schedule(0.0, AutopilotEnd);
        }
        else
        {
            eventDriver.Schedule(0.0); // Schedule main loop
        }
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
