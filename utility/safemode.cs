public class SafeMode : DockingHandler
{
    private const double RunDelay = 1.0;

    private readonly SafeModeHandler[] SafeModeHandlers;

    private readonly TimeSpan AbandonmentTimeout = TimeSpan.Parse(ABANDONMENT_TIMEOUT);

    private bool? IsControlled = null;
    private bool IsDocked = true;
    private DateTime LastControlled;
    private bool Abandoned = false;

    public SafeMode(params SafeModeHandler[] safeModeHandlers)
    {
        SafeModeHandlers = safeModeHandlers;
        LastControlled = DateTime.UtcNow;
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
            eventDriver.Schedule(RunDelay, Run);
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        IsDocked = false;
        eventDriver.Schedule(0.0, Run);
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

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return; // Don't bother if we're docked

        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks, controller => controller.IsFunctional);
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

        if (ABANDONMENT_ENABLED)
        {
            // Abandonment check
            if (!(bool)IsControlled)
            {
                var abandonTime = commons.Now - AbandonmentTimeout;

                if (!Abandoned && LastControlled <= abandonTime)
                {
                    Abandoned = true;
                    TriggerSafeMode(commons, eventDriver);
                }
            }
            else
            {
                ResetAbandonment(commons);
            }
            // commons.Echo("Timeout: " + AbandonmentTimeout);
            // commons.Echo("Last Controlled: " + LastControlled);
        }

        eventDriver.Schedule(RunDelay, Run);
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

    private void TriggerSafeMode(ZACommons commons, EventDriver eventDriver,
                                 string timerBlockName = SAFE_MODE_NAME)
    {
        for (var i = 0; i < SafeModeHandlers.Length; i++)
        {
            SafeModeHandlers[i].SafeMode(commons, eventDriver);
        }
        ZACommons.StartTimerBlockWithName(commons.Blocks, timerBlockName);
    }
}
