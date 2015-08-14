public class DockingManager
{
    private bool? IsConnected = null;

    public void Run(ZACommons commons, bool? isConnected = null)
    {
        var currentState = isConnected != null ? (bool)isConnected :
            ZACommons.IsConnectedAnywhere(commons.Blocks);

        if (IsConnected == null)
        {
            IsConnected = !currentState; // And fall through below
        }

        // Only bother if there was a change in state
        if (IsConnected != currentState)
        {
            IsConnected = currentState;

            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyThrust>(commons.Blocks), !(bool)IsConnected);
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyGyro>(commons.Blocks), !(bool)IsConnected);
            ZACommons.SetBatteryRecharge(ZACommons.GetBlocksOfType<IMyBatteryBlock>(commons.Blocks), (bool)IsConnected);

            if (TOUCH_ANTENNA) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks), !(bool)IsConnected);
            if (TOUCH_LANTENNA) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyLaserAntenna>(commons.Blocks), !(bool)IsConnected);
            if (TOUCH_BEACON) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyBeacon>(commons.Blocks), !(bool)IsConnected);
            if (TOUCH_LIGHTS) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyLightingBlock>(commons.Blocks), !(bool)IsConnected);
        }
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        if (command == "undock")
        {
            // Just a cheap way to avoid using a timer block. Turn off all
            // connectors and unlock all landing gear.
            // I added this because 'P' sometimes unlocks other ships as well...
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks, connector => connector.DefinitionDisplayNameText == "Connector"),
                                   false); // Avoid Ejectors

            var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
            for (var e = gears.GetEnumerator(); e.MoveNext();)
            {
                var gear = e.Current;
                if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
            }
        }
        else if (command == "dock")
        {
            // Enable all connectors
            ZACommons.ForEachBlockOfType<IMyShipConnector>(commons.Blocks,
                                                           connector =>
                    {
                        if (connector.IsFunctional &&
                            connector.DefinitionDisplayNameText == "Connector")
                        {
                            connector.SetValue<bool>("OnOff", true);
                        }
                    });

            // 1 second from now, lock connectors that are ready
            eventDriver.Schedule(1.0, (c, ed) =>
                    {
                        ZACommons.ForEachBlockOfType<IMyShipConnector>(c.Blocks,
                                                                       connector =>
                                {
                                    if (connector.IsFunctional &&
                                        connector.DefinitionDisplayNameText == "Connector" &&
                                        connector.IsLocked && !connector.IsConnected)
                                    {
                                        connector.GetActionWithName("Lock").Apply(connector);
                                    }
                                });
                    });

            // And 2 seconds from now, lock landing gear
            eventDriver.Schedule(2.0, (c, ed) =>
                    {
                        ZACommons.ForEachBlockOfType<IMyLandingGear>(c.Blocks,
                                                                     gear =>
                                {
                                    if (gear.IsFunctional && gear.IsWorking &&
                                        gear.Enabled && !gear.IsLocked) gear.GetActionWithName("Lock").Apply(gear);
                                });
                    });
        }
    }
}
