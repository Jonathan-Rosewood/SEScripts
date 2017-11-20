//@ commons eventdriver
public class WeaponTrigger
{
    private Action<ZACommons, EventDriver> TriggerAction;

    public bool Triggered { get; private set; }

    public WeaponTrigger()
    {
        Triggered = false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> triggerAction)
    {
        TriggerAction = triggerAction;
        Triggered = false;
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        if (Triggered) return;
        argument = argument.Trim().ToLower();
        if (argument == "firefirefire")
        {
            Triggered = true;
            TriggerAction(commons, eventDriver);
        }
    }
}
