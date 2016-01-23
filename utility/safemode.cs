public class SafeMode : DockingHandler
{
    public interface EmergencyStopHandler
    {
        void EmergencyStop(ZACommons commons, EventDriver eventDriver);
    }

    private const double RunDelay = 1.0;

    private readonly EmergencyStopHandler emergencyStopHandler;

    private readonly TimeSpan AbandonmentTimeout = TimeSpan.Parse(ABANDONMENT_TIMEOUT);

    private bool IsControlled = false;
    private bool IsDocked = true;
    private TimeSpan LastControlled = TimeSpan.FromSeconds(0); // Hmm, TimeSpan.Zero doesn't work?
    private bool Abandoned = false;

    public SafeMode(EmergencyStopHandler emergencyStopHandler = null)
    {
        this.emergencyStopHandler = emergencyStopHandler;
    }

    public void Docked(ZACommons commons, EventDriver eventDriver)
    {
        IsDocked = true;
    }

    public void Undocked(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked)
        {
            IsControlled = false;

            ResetAbandonment();

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

    private void ResetAbandonment()
    {
        LastControlled = TimeSpan.FromSeconds(0);
        Abandoned = false;
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return; // Don't bother if we're docked

        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks, controller => controller.IsFunctional);
        var currentState = IsShipControlled(controllers);

        // Flight safety stuff, only check on state change
        if (IsControlled != currentState)
        {
            IsControlled = currentState;

            if (!IsControlled)
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
                    if (emergencyStopHandler != null) emergencyStopHandler.EmergencyStop(commons, eventDriver);
                    ZACommons.StartTimerBlockWithName(commons.Blocks, EMERGENCY_STOP_NAME);
                }
            }
        }

        if (ABANDONMENT_ENABLED)
        {
            // Abandonment check
            if (!IsControlled)
            {
                LastControlled += commons.Program.ElapsedTime;

                if (!Abandoned && LastControlled >= AbandonmentTimeout)
                {
                    Abandoned = true;
                    ZACommons.StartTimerBlockWithName(commons.Blocks, SAFE_MODE_NAME);
                }
            }
            else
            {
                ResetAbandonment();
            }
            // commons.Echo("Timeout: " + AbandonmentTimeout);
            // commons.Echo("Last Controlled: " + LastControlled);
        }

        eventDriver.Schedule(RunDelay, Run);
    }
}
