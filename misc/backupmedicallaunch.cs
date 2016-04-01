//@ commons eventdriver
public class BackupMedicalLaunch
{
    private Action<ZACommons, EventDriver> PostLaunch = null;

    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(commons.Blocks);
        if (remotes.Count != 1)
        {
            throw new Exception("Expecting exactly 1 remote control");
        }
        return (IMyRemoteControl)remotes[0];
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> postLaunch = null)
    {
        PostLaunch = postLaunch;

        var remote = GetRemoteControl(commons);
        // Determine current state
        if (remote.GetValue<bool>("AutoPilot"))
        {
            eventDriver.Schedule(0.0, AutopilotEnd);
        }
        else
        {
            if (PostLaunch != null) PostLaunch(commons, eventDriver);
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
            // All done.
            if (PostLaunch != null) PostLaunch(commons, eventDriver);
        }
    }
}
