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
            // Disable tools if we just docked
            if (TOUCH_TOOLS && (bool)IsConnected) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyShipToolBase>(commons.Blocks), false);
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
                var gear = (IMyLandingGear)e.Current;
                if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
            }
        }
        else if (command == "dock")
        {
            // Enable all connectors
            ZACommons.ForEachBlockOfType<IMyShipConnector>(commons.Blocks,
                                                           block =>
                    {
                        var connector = (IMyShipConnector)block;
                        if (connector.IsFunctional &&
                            connector.DefinitionDisplayNameText == "Connector")
                        {
                            connector.SetValue<bool>("OnOff", true);
                        }
                    });

            // 1 second from now, lock connectors that are ready
            eventDriver.Schedule(1.0, (c, ed) =>
                    {
                        bool connected = false;
                        ZACommons.ForEachBlockOfType<IMyShipConnector>(c.Blocks,
                                                                       block =>
                                {
                                    var connector = (IMyShipConnector)block;
                                    if (connector.IsFunctional &&
                                        connector.DefinitionDisplayNameText == "Connector" &&
                                        connector.IsLocked && !connector.IsConnected)
                                    {
                                        connector.GetActionWithName("Lock").Apply(connector);
                                        connected = true;
                                    }
                                });

                        if (connected)
                        {
                            // And 1 second from now, lock landing gear
                            ed.Schedule(1.0, (c2, ed2) =>
                                    {
                                        ZACommons.ForEachBlockOfType<IMyLandingGear>(c2.Blocks,
                                                                                     block =>
                                                {
                                                    var gear = (IMyLandingGear)block;
                                                    if (gear.IsFunctional && gear.IsWorking &&
                                                        gear.Enabled && !gear.IsLocked) gear.GetActionWithName("Lock").Apply(gear);
                                                });
                                    });
                        }
                    });
        }
    }
}
