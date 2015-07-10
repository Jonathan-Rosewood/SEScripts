public class BatteryMonitor
{
    public void Run(MyGridProgram program, ZALibrary.Ship ship,
                    bool? isConnected = null)
    {
        var lowBattery = ship.GetBlockWithName<IMyTimerBlock>(LOW_BATTERY_NAME);
        // Don't bother if there's no timer block
        if (lowBattery == null) return;

        var batteries = ship.GetBlocksOfType<IMyBatteryBlock>();

        // Avoid divide-by-zero in case there are no batteries
        if (batteries.Count == 0) return;

        var currentStoredPower = 0.0f;
        var maxStoredPower = 0.0f;

        // Hmm, doesn't check battery recharge state...
        for (var e = batteries.GetEnumerator(); e.MoveNext();)
        {
            var battery = e.Current;

            currentStoredPower += battery.CurrentStoredPower;
            maxStoredPower += battery.MaxStoredPower;
        }

        var batteryPercent = currentStoredPower / maxStoredPower;

        var connected = isConnected != null ? (bool)isConnected :
            ship.IsConnectedAnywhere();

        if (lowBattery.Enabled && !lowBattery.IsCountingDown && batteryPercent < BATTERY_THRESHOLD &&
            !connected)
        {
            lowBattery.GetActionWithName("Start").Apply(lowBattery);
        }
        else if (!lowBattery.Enabled && batteryPercent >= BATTERY_THRESHOLD)
        {
            lowBattery.GetActionWithName("OnOff_On").Apply(lowBattery);
        }
    }
}
