public class DockingManager
{
    private bool? IsConnected = null;

    public void Run(MyGridProgram program, ZALibrary.Ship ship, string argument,
                    bool? isConnected = null)
    {
        var currentState = isConnected != null ? (bool)isConnected :
            ship.IsConnectedAnywhere();

        if (IsConnected == null)
        {
            IsConnected = !currentState; // And fall through below
        }

        // Only bother if there was a change in state
        if (IsConnected != currentState)
        {
            IsConnected = currentState;

            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyThrust>(), !(bool)IsConnected);
            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyGyro>(), !(bool)IsConnected);
            ZALibrary.SetBatteryRecharge(ship.GetBlocksOfType<IMyBatteryBlock>(), (bool)IsConnected);

            if (TOUCH_ANTENNA) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyRadioAntenna>(), !(bool)IsConnected);
            if (TOUCH_LANTENNA) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyLaserAntenna>(), !(bool)IsConnected);
            if (TOUCH_BEACON) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyBeacon>(), !(bool)IsConnected);
            if (TOUCH_LIGHTS) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyLightingBlock>(), !(bool)IsConnected);
        }

        var command = argument.Trim().ToLower();
        if (command == "undock")
        {
            // Just a cheap way to avoid using a timer block. Turn off all
            // connectors and unlock all landing gear.
            // I added this because 'P' sometimes unlocks other ships as well...
            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyShipConnector>(connector => connector.DefinitionDisplayNameText == "Connector"),
                                   false); // Avoid Ejectors

            var gears = ship.GetBlocksOfType<IMyLandingGear>();
            for (var e = gears.GetEnumerator(); e.MoveNext();)
            {
                var gear = e.Current;
                if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
            }
        }
    }
}
