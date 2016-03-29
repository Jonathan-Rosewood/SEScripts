public class SafeMode : DockingHandler
{
    private const double FastRunDelay = 1.0;
    private const double SlowRunDelay = 5.0;

    private readonly SafeModeHandler[] SafeModeHandlers;

    private readonly TimeSpan AbandonmentTimeout = TimeSpan.Parse(ABANDONMENT_TIMEOUT);

    private bool? IsControlled = null;
    private bool IsDocked = true;
    private DateTime LastControlled;
    public bool Abandoned { get; private set; }

    public SafeMode(params SafeModeHandler[] safeModeHandlers)
    {
        SafeModeHandlers = safeModeHandlers;
        LastControlled = DateTime.UtcNow;
        Abandoned = false;
    }

    public void Docked(ZACommons commons, EventDriver eventDriver)
    {
        IsDocked = true;
    }

    public void Undocked(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked)
        {
            IsControlled = null;

            ResetAbandonment(commons);

            IsDocked = false;
            eventDriver.Schedule(FastRunDelay, Fast);
            eventDriver.Schedule(SlowRunDelay, Slow);
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        IsControlled = null;

        ResetAbandonment(commons);

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
                for (var e = controllers.GetEnumerator(); e.MoveNext();)
                {
                    var controller = (IMyShipController)e.Current;
                    if (!controller.DampenersOverride)
                    {
                        controller.GetActionWithName("DampenersOverride").Apply(controller);
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

        if (currentState) ResetAbandonment(commons);

        if (ABANDONMENT_ENABLED)
        {
            // Abandonment check
            if (!Abandoned && !currentState)
            {
                var abandonTime = commons.Now - AbandonmentTimeout;

                if (LastControlled <= abandonTime)
                {
                    TriggerSafeMode(commons, eventDriver);
                }
            }
            // commons.Echo("Timeout: " + AbandonmentTimeout);
            // commons.Echo("Last Controlled: " + LastControlled);
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
                for (var e = controllers.GetEnumerator(); e.MoveNext();)
                {
                    var remote = e.Current as IMyRemoteControl;
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

    private bool IsValidController(IMyTerminalBlock block)
    {
        var controller = block as IMyShipController;
        if (controller == null || !controller.IsFunctional) return false;
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

    private bool IsShipControlled(IEnumerable<IMyTerminalBlock> controllers)
    {
        for (var e = controllers.GetEnumerator(); e.MoveNext();)
        {
            var controller = e.Current as IMyShipController;
            if (controller != null && controller.IsUnderControl)
            {
                return true;
            }
        }
        return false;
    }

    private void ResetAbandonment(ZACommons commons)
    {
        LastControlled = commons.Now;
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
        for (var e = commons.Blocks.GetEnumerator(); e.MoveNext();)
        {
            var antenna = e.Current as IMyRadioAntenna;
            if (antenna != null && antenna.IsWorking && antenna.Enabled) // && antenna.IsBroadcasting)
            {
                antennaFound = true;
                break;
            }

            var lantenna = e.Current as IMyLaserAntenna;
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
