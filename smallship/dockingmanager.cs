public class DockingManager
{
    private bool FirstRun = true;
    private bool IsConnected;

    public void Run(MyGridProgram program, ZALibrary.Ship ship, string argument,
                    bool? isConnected = null)
    {
        var currentState = isConnected != null ? (bool)isConnected :
            ship.IsConnectedAnywhere();

        if (FirstRun)
        {
            FirstRun = false;
            IsConnected = !currentState; // And fall through below
        }

        // Only bother if there was a change in state
        if (IsConnected != currentState)
        {
            IsConnected = currentState;

            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyThrust>(), !IsConnected);
            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyGyro>(), !IsConnected);
            ZALibrary.SetBatteryRecharge(ship.GetBlocksOfType<IMyBatteryBlock>(), IsConnected);

            if (TOUCH_ANTENNA) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyRadioAntenna>(), !IsConnected);
            if (TOUCH_LANTENNA) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyLaserAntenna>(), !IsConnected);
            if (TOUCH_BEACON) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyBeacon>(), !IsConnected);
            if (TOUCH_LIGHTS) ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyLightingBlock>(), !IsConnected);
        }

        var command = argument.Trim().ToLower();
        if (command == "undock")
        {
            // Just a cheap way to avoid using a timer block. Turn off all
            // connectors and unlock all landing gear.
            // I added this because 'P' sometimes unlocks other ships as well...
            ZALibrary.EnableBlocks(ship.GetBlocksOfType<IMyShipConnector>(delegate (IMyShipConnector connector)
                                                                          {
                                                                              return connector.DefinitionDisplayNameText == "Connector"; // Avoid Ejectors
                                                                          }), false);

            var gears = ship.GetBlocksOfType<IMyLandingGear>();
            for (var e = gears.GetEnumerator(); e.MoveNext();)
            {
                var gear = e.Current;
                if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
            }
        }
    }
}
