// Assumptions: No reactor, only batteries and solar panels
// Solar panels can power entire system no problem
// Thus batteries only serve as backup. They should almost always be discharging
// (because output from solar panels will be prioritized anyway).
public class BatteryManager
{
    public struct AggregateBatteryDetails
    {
        public float CurrentPowerOutput;
        public float MaxPowerOutput;
        public float CurrentStoredPower;
        public float MaxStoredPower;

        public AggregateBatteryDetails(IEnumerable<IMyBatteryBlock> batteries)
        {
            CurrentPowerOutput = 0.0f;
            MaxPowerOutput = 0.0f;
            CurrentStoredPower = 0.0f;
            MaxStoredPower = 0.0f;

            for (var e = batteries.GetEnumerator(); e.MoveNext();)
            {
                var battery = e.Current;
                CurrentPowerOutput += battery.CurrentPowerOutput;
                MaxPowerOutput += battery.MaxPowerOutput;
                CurrentStoredPower += battery.CurrentStoredPower;
                MaxStoredPower += battery.MaxStoredPower;
            }
        }
    }

    public interface PowerDrainHandler
    {
        void PowerDrainStarted(ZALibrary.Ship ship);
        void PowerDrainEnded(ZALibrary.Ship ship);
    }

    private const int STATE_NORMAL = 0;
    private const int STATE_RECHARGE = 1;

    private readonly TimeSpan DischargeInterval = TimeSpan.Parse(BATTERY_MANAGER_DISCHARGE_INTERVAL);
    private readonly TimeSpan RechargeInterval = TimeSpan.Parse(BATTERY_MANAGER_RECHARGE_INTERVAL);
    
    private int? CurrentState = null;
    private TimeSpan SinceLastStateChange;

    private PowerDrainHandler powerDrainHandler;

    private bool Draining = false;
    private uint DrainCounts = 0;
    private readonly LinkedList<bool> DrainData = new LinkedList<bool>();

    public BatteryManager(PowerDrainHandler powerDrainHandler = null)
    {
        this.powerDrainHandler = powerDrainHandler;
    }

    private bool AddDrainData(bool draining)
    {
        DrainData.AddLast(draining);
        if (draining) DrainCounts++;
        while (DrainData.Count > BATTERY_MANAGER_DRAIN_CHECK_TICKS)
        {
            // Forget about oldest value, adjust count if necessary
            if (DrainData.First.Value) DrainCounts--;
            DrainData.RemoveFirst();
        }

        return DrainCounts >= BATTERY_MANAGER_DRAIN_CHECK_THRESHOLD;
    }

    public void Run(MyGridProgram program, ZALibrary.Ship ship)
    {
        var batteries = ship.GetBlocksOfType<IMyBatteryBlock>(delegate (IMyBatteryBlock battery)
                                                              {
                                                                  return battery.IsFunctional && battery.Enabled;
                                                              });


        if (CurrentState == null)
        {
            // First time run, get to known state and return
            CurrentState = STATE_NORMAL;
            SinceLastStateChange = TimeSpan.FromSeconds(0);
            ZALibrary.SetBatteryRecharge(batteries, false);
            return;
        }

        SinceLastStateChange += program.ElapsedTime;

        var aggregateDetails = new AggregateBatteryDetails(batteries);

        switch (CurrentState)
        {
            case STATE_NORMAL:
                if (SinceLastStateChange >= DischargeInterval)
                {
                    // Don't check again until next interval, regardless of whether we
                    // change state
                    SinceLastStateChange -= DischargeInterval;

                    // Only recharge if there is available power, e.g. the batteries have no load,
                    // and there is need to
                    if (aggregateDetails.CurrentPowerOutput == 0.0f &&
                        aggregateDetails.CurrentStoredPower < aggregateDetails.MaxStoredPower)
                    {
                        CurrentState = STATE_RECHARGE;
                        ZALibrary.SetBatteryRecharge(batteries, true);
                    }
                }
                break;
            case STATE_RECHARGE:
                // Too bad we don't have access to battery input (w/o parsing DetailInfo)
                // Then we could figure out non-battery load and cancel recharge pre-emptively
                // when needed
                if (SinceLastStateChange >= RechargeInterval)
                {
                    CurrentState = STATE_NORMAL;
                    SinceLastStateChange -= RechargeInterval;
                    ZALibrary.SetBatteryRecharge(batteries, false);
                }
                break;
        }

        // See if we have a net power loss
        var newDraining = AddDrainData(aggregateDetails.CurrentPowerOutput > 0.0f);
        if (powerDrainHandler != null)
        {
            if (!Draining && newDraining)
            {
                powerDrainHandler.PowerDrainStarted(ship);
            }
            else if (Draining && !newDraining)
            {
                powerDrainHandler.PowerDrainEnded(ship);
            }
        }
            
        Draining = newDraining;

        program.Echo(String.Format("Total Stored Power: {0}h", ZALibrary.FormatPower(aggregateDetails.CurrentStoredPower)));
        program.Echo(String.Format("Max Stored Power: {0}h", ZALibrary.FormatPower(aggregateDetails.MaxStoredPower)));
        if (Draining) program.Echo("Net power loss!");
    }
}
