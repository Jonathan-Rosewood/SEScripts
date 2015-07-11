// Assumptions: This is a large ship/station, so there will be reactors, batteries, and
// perhaps solar panels.
// Batteries only store 80% of the power from *any* source (look at MyBatteryBlock code)
// So it only makes sense to use them during emergencies, especially if your ship/station
// is not entirely solar-powered.
// Also because of this fact, it does not make sense to have batteries in mixed charge/
// recharge states.
// So the rule is: If you have batteries discharging, any non-full recharging battery must
// be turned off.
public class PowerManager
{
    public class BatteryComparer : IComparer<IMyBatteryBlock>
    {
        private readonly bool Desc;

        public BatteryComparer(bool desc)
        {
            Desc = desc;
        }

        public int Compare(IMyBatteryBlock a, IMyBatteryBlock b)
        {
            if (Desc)
            {
                return b.CurrentStoredPower.CompareTo(a.CurrentStoredPower); // reversed
            }
            else
            {
                return a.CurrentStoredPower.CompareTo(b.CurrentStoredPower);
            }
        }
    }

    public struct PowerDetails
    {
        public float CurrentPowerOutput;
        public float MaxPowerOutput;

        public static PowerDetails operator+(PowerDetails a, PowerDetails b)
        {
            var result = new PowerDetails();
            result.CurrentPowerOutput = a.CurrentPowerOutput + b.CurrentPowerOutput;
            result.MaxPowerOutput = a.MaxPowerOutput + b.MaxPowerOutput;
            return result;
        }
    }

    private readonly BatteryComparer batteryComparer = new BatteryComparer(false);
    private readonly BatteryComparer batteryComparerDesc = new BatteryComparer(true);

    private readonly TimeSpan QuietTimeout = TimeSpan.Parse(POWER_MANAGER_QUIET_TIMEOUT);
    private TimeSpan QuietTimer = TimeSpan.FromSeconds(0);
    
    public PowerDetails GetPowerDetails<T>(List<T> producers)
        where T : IMyTerminalBlock
    {
        var result = new PowerDetails();
        result.CurrentPowerOutput = 0.0f;
        result.MaxPowerOutput = 0.0f;
        for (var e = producers.GetEnumerator(); e.MoveNext();)
        {
            var producer = e.Current as IMyPowerProducer;
            if (e.Current.IsFunctional && e.Current.IsWorking &&
                producer != null)
            {
                result.CurrentPowerOutput += producer.CurrentPowerOutput;
                result.MaxPowerOutput += producer.MaxPowerOutput;
            }
        }
        return result;
    }

    public void Run(MyGridProgram program, List<IMyTerminalBlock> ship)
    {
        // Limit to just functional batteries on the same grid (don't care if it's on or off)
        var batteries = ZALibrary.GetBlocksOfType<IMyBatteryBlock>(ship,
                                                                   delegate (IMyBatteryBlock battery)
                                                                   {
                                                                       return battery.CubeGrid == program.Me.CubeGrid && battery.IsFunctional;
                                                                   });

        // Grab all power producers everywhere, except batteries on this ship
        var producers = ZALibrary.GetBlocksOfType<IMyTerminalBlock>(ship,
                                                                    delegate (IMyTerminalBlock block)
                                                                    {
                                                                        return block is IMyPowerProducer && (block.CubeGrid != program.Me.CubeGrid || !(block is IMyBatteryBlock));
                                                                    });

        var batteryDetails = GetPowerDetails<IMyBatteryBlock>(batteries);
        var producerDetails = GetPowerDetails<IMyTerminalBlock>(producers);

        var totalDetails = batteryDetails + producerDetails;

        // First, the degenerate cases...
        if (totalDetails.MaxPowerOutput == 0.0f) return; // Don't think this is possible...
        if (producerDetails.MaxPowerOutput == 0.0f)
        {
            // Nothing but our batteries. Just put all batteries online.
            ZALibrary.EnableBlocks(batteries, true);
            ZALibrary.SetBatteryRecharge(batteries, false);
            return;
        }
        
        var totalLoad = totalDetails.CurrentPowerOutput / totalDetails.MaxPowerOutput;

        // If total system load exceeds threshold, attempt to bring a battery online.
        if (totalLoad > POWER_MANAGER_HIGH_LOAD_THRESHOLD)
        {
            QuietTimer = TimeSpan.FromSeconds(0);

            // Sort by stored power descending
            batteries.Sort(batteryComparerDesc);

            // Bring the first recharging battery online, shut down all other recharging batteries
            // (just shutting them down would probably free up a lot of power... need
            // separate case?)
            var found = false;
            for (var e = batteries.GetEnumerator(); e.MoveNext();)
            {
                var battery = e.Current;

                if (battery.Enabled && battery.ProductionEnabled) continue; // Battery is already online, don't touch it

                if (!found)
                {
                    // Found the first one, bring it online
                    if (!battery.Enabled) battery.GetActionWithName("OnOff_On").Apply(battery);
                    ZALibrary.SetBatteryRecharge(battery, false);
                    found = true;
                }
                else
                {
                    // Shut it down
                    if (battery.Enabled) battery.GetActionWithName("OnOff_Off").Apply(battery);
                }
            }
        }
        else if (totalLoad < POWER_MANAGER_LOW_LOAD_THRESHOLD)
        {
            QuietTimer += program.ElapsedTime;

            if (QuietTimer >= QuietTimeout)
            {
                // NB We don't reset QuietTimer, so we will trigger at next tick again
                // (assuming load is still low)

                // Sort from low to high
                batteries.Sort(batteryComparer);

                // Any batteries actively discharging?
                if (batteryDetails.CurrentPowerOutput > 0.0f)
                {
                    // Take a battery offline, starting with the least charged.
                    // Note, we cannot actually start recharging until they are all offline
                    for (var e = batteries.GetEnumerator(); e.MoveNext();)
                    {
                        var battery = e.Current;

                        if (battery.Enabled && battery.ProductionEnabled)
                        {
                            battery.GetActionWithName("OnOff_Off").Apply(battery);
                            ZALibrary.SetBatteryRecharge(battery, true);
                            break;
                        }
                    }
                }
                else
                {
                    // All batteries are down, enable one for charging
                    // (repeat next tick until we break low threshold, in which case QuietTimer
                    // will reset)
                    for (var e = batteries.GetEnumerator(); e.MoveNext();)
                    {
                        var battery = e.Current;

                        if (!battery.Enabled || battery.ProductionEnabled /* huh?! */)
                        {
                            if (!battery.Enabled) battery.GetActionWithName("OnOff_On").Apply(battery);
                            ZALibrary.SetBatteryRecharge(battery, true);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            QuietTimer = TimeSpan.FromSeconds(0);
        }
    }
}
