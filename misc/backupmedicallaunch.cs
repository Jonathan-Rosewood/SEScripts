public class BackupMedicalLaunch
{
    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(commons.Blocks);
        if (remotes.Count != 1)
        {
            throw new Exception("Expecting exactly 1 remote control");
        }
        return (IMyRemoteControl)remotes[0];
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var remote = GetRemoteControl(commons);
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
