public class SafeMode
{
    private bool FirstRun = true;
    private bool IsControlled;

    private bool PreviouslyDocked;
    private TimeSpan AbandonmentTimeout;
    private TimeSpan LastControlled = TimeSpan.FromSeconds(0); // Hmm, TimeSpan.Zero doesn't work?
    private bool Abandoned = false;

    private bool IsShipControlled(IEnumerable<IMyShipController> controllers)
    {
        for (var e = controllers.GetEnumerator(); e.MoveNext();)
        {
            if (e.Current.IsUnderControl)
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

    public void Run(MyGridProgram program, ZALibrary.Ship ship,
                    bool? isConnected = null)
    {
        var connected = isConnected != null ? (bool)isConnected :
            ship.IsConnectedAnywhere();
        if (connected)
        {
            PreviouslyDocked = true;
            return; // Don't bother if we're docked
        }

        var controllers = ship.GetBlocksOfType<IMyShipController>();
        var currentState = IsShipControlled(controllers);

        if (FirstRun)
        {
            FirstRun = false;
            IsControlled = currentState;
            if (!TimeSpan.TryParse(ABANDONMENT_TIMEOUT, out AbandonmentTimeout))
            {
                program.Echo("Invalid abandonment timeout, defaulting to 1 hour");
                AbandonmentTimeout = TimeSpan.FromHours(1);
            }
            return;
        }

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
                    var controller = e.Current;
                    if (!controller.DampenersOverride)
                    {
                        controller.GetActionWithName("DampenersOverride").Apply(controller);
                        dampenersChanged = true;
                    }
                }

                // Only trigger timer block if dampeners were actually engaged
                if (dampenersChanged) ship.StartTimerBlockWithName(EMERGENCY_STOP_NAME);
            }
        }

        // Reset abandonment stuff if we just undocked
        if (PreviouslyDocked)
        {
            PreviouslyDocked = false;
            ResetAbandonment();
        }

        // Abandonment check
        if (!IsControlled)
        {
            LastControlled += program.ElapsedTime;

            if (!Abandoned && LastControlled >= AbandonmentTimeout)
            {
                Abandoned = true;
                ship.StartTimerBlockWithName(SAFE_MODE_NAME);
            }
        }
        else
        {
            ResetAbandonment();
        }
        // program.Echo("Timeout: " + AbandonmentTimeout);
        // program.Echo("Last Controlled: " + LastControlled);
    }
}
