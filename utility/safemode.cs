//@ commons eventdriver dockinghandler safemodehandler
public class SafeMode : DockingHandler
{
    private const double FastRunDelay = 1.0;
    private const double SlowRunDelay = 5.0;

    private readonly SafeModeHandler[] SafeModeHandlers;

    private readonly TimeSpan AbandonmentTimeout = TimeSpan.Parse(ABANDONMENT_TIMEOUT);

    private bool? IsControlled = null;
    private bool IsDocked = true;
    private TimeSpan AbandonedTime;
    public bool Abandoned { get; private set; }

    public SafeMode(params SafeModeHandler[] safeModeHandlers)
    {
        SafeModeHandlers = safeModeHandlers;
        AbandonedTime = AbandonmentTimeout;
        Abandoned = false;
    }

    public void PreDock(ZACommons commons, EventDriver eventDriver) { }

    public void DockingAction(ZACommons commons, EventDriver eventDriver,
                              bool docked)
    {
        if (docked)
        {
            IsDocked = true;
        }
        else if (IsDocked)
        {
            IsControlled = null;

            ResetAbandonment(eventDriver);

            IsDocked = false;
            eventDriver.Schedule(FastRunDelay, Fast);
            eventDriver.Schedule(SlowRunDelay, Slow);
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        IsControlled = null;

        ResetAbandonment(eventDriver);

        IsDocked = false;
        eventDriver.Schedule(0.0, Fast);
        eventDriver.Schedule(0.0, Slow);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        if (command == "safemode")
        {
            TriggerSafeMode(commons, eventDriver);
        }
    }

    public void Fast(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return; // Don't bother if we're docked

        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks, controller => IsValidController(controller));
        var currentState = IsShipControlled(controllers);

        // Flight safety stuff, only check on state change
        if (IsControlled == null || (bool)IsControlled != currentState)
        {
            IsControlled = currentState;

            if (!(bool)IsControlled)
            {
                var dampenersChanged = false;

                // Enable dampeners
                foreach (var block in controllers)
                {
                    var controller = (IMyShipController)block;
                    if (!controller.DampenersOverride)
                    {
                        controller.SetValue<bool>("DampenersOverride", true);
                        dampenersChanged = true;
                    }
                }

                // Only do something if dampeners were actually engaged
                if (dampenersChanged)
                {
                    TriggerSafeMode(commons, eventDriver, EMERGENCY_STOP_NAME);
                }
            }
        }

        if (currentState) ResetAbandonment(eventDriver);

        if (ABANDONMENT_ENABLED)
        {
            // Abandonment check
            if (!Abandoned && !currentState)
            {
                if (AbandonedTime <= eventDriver.TimeSinceStart)
                {
                    TriggerSafeMode(commons, eventDriver);
                }
            }
            //commons.Echo("AbandonedTime:  " + AbandonedTime);
            //commons.Echo("TimeSinceStart: " + eventDriver.TimeSinceStart);
        }

        eventDriver.Schedule(FastRunDelay, Fast);
    }

    public void Slow(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return; // Don't bother if we're docked

        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks, controller => IsValidController(controller));

        if (!Abandoned)
        {
            // No functional controllers => same as abandoned
            if (controllers.Count == 0)
            {
                TriggerSafeMode(commons, eventDriver);
            }
            else
            {
                // We have any non-remotes?
                var nonRemote = false;
                foreach (var block in controllers)
                {
                    var remote = block as IMyRemoteControl;
                    if (remote == null)
                    {
                        nonRemote = true;
                        break;
                    }
                }

                if (!nonRemote)
                {
                    // Only remote controls on-board. Do we have an
                    // active antenna?
                    TriggerIfNoAntenna(commons, eventDriver);
                }
            }
        }

        eventDriver.Schedule(SlowRunDelay, Slow);
    }

    private bool IsValidController(IMyShipController controller)
    {
        if (!controller.IsFunctional) return false;
        // The only sane one
        if (controller is IMyRemoteControl) return true;
        // Grumble grumble
        switch (controller.DefinitionDisplayNameText)
        {
            case "Flight Seat":
                return true;
            case "Control Station":
                return true;
            case "Cockpit":
                return true;
            case "Fighter Cockpit":
                return true;
        }
        return false;
    }

    private bool IsShipControlled(IEnumerable<IMyShipController> controllers)
    {
        foreach (var controller in controllers)
        {
            if (controller.IsUnderControl)
            {
                return true;
            }
        }
        return false;
    }

    private void ResetAbandonment(EventDriver eventDriver)
    {
        AbandonedTime = eventDriver.TimeSinceStart + AbandonmentTimeout;
        Abandoned = false;
    }

    private void TriggerSafeMode(ZACommons commons, EventDriver eventDriver,
                                 string timerBlockName = SAFE_MODE_NAME)
    {
        Abandoned = true; // No need to trigger any other condition until reset

        for (var i = 0; i < SafeModeHandlers.Length; i++)
        {
            SafeModeHandlers[i].SafeMode(commons, eventDriver);
        }

        ZACommons.StartTimerBlockWithName(commons.Blocks, timerBlockName);
    }

    private void TriggerIfNoAntenna(ZACommons commons, EventDriver eventDriver)
    {
        // NB Race condition with RedundancyManager, but oh well.

        // Look for functioning antenna or laser antenna
        var antennaFound = false;
        foreach (var block in commons.Blocks)
        {
            var antenna = block as IMyRadioAntenna;
            if (antenna != null && antenna.IsWorking && antenna.Enabled) // && antenna.IsBroadcasting)
            {
                antennaFound = true;
                break;
            }

            var lantenna = block as IMyLaserAntenna;
            // Unfortunately, can't know if lantenna is connected
            // w/o parsing DetailedInfo.
            // So just check IsOutsideLimits.
            if (lantenna != null && lantenna.IsWorking && lantenna.Enabled && !lantenna.IsOutsideLimits)
            {
                antennaFound = true;
                break;
            }
        }

        if (!antennaFound) TriggerSafeMode(commons, eventDriver); // We're deaf...
    }

    public void TriggerIfUncontrolled(ZACommons commons, EventDriver eventDriver)
    {
        if (!Abandoned && IsControlled != null && !(bool)IsControlled)
        {
            TriggerSafeMode(commons, eventDriver);
        }
    }
}
