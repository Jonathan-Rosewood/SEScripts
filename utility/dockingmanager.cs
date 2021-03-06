//@ commons eventdriver dockinghandler
public class DockingManager
{
    private const double RunDelay = 10.0;

    private DockingHandler[] DockingHandlers;

    private bool IsDocked;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     params DockingHandler[] dockingHandlers)
    {
        DockingHandlers = dockingHandlers;
        var docked = IsConnectedAnywhere(commons.Blocks);
        ManageShip(commons, eventDriver, docked);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        switch (command)
        {
            case "dock":
                DockStart(commons, eventDriver);
                break;
            case "undock":
                UndockStart(commons, eventDriver);
                break;
        }
    }

    public void DockStart(ZACommons commons, EventDriver eventDriver)
    {
        // Enable all connectors
        ZACommons.ForEachBlockOfType<IMyShipConnector>(commons.Blocks,
                                                       connector =>
                {
                    if (connector.IsFunctional &&
                        connector.DefinitionDisplayNameText == "Connector")
                    {
                        connector.Enabled = true;
                    }
                });

        // Perform any pre-docking actions, if any
        for (var i = 0; i < DockingHandlers.Length; i++)
        {
            DockingHandlers[i].PreDock(commons, eventDriver);
        }

        // 1 second from now, lock connectors that are ready
        eventDriver.Schedule(1.0, DockLock);
    }

    public void DockLock(ZACommons commons, EventDriver eventDriver)
    {
        bool connected = false;
        ZACommons.ForEachBlockOfType<IMyShipConnector>(commons.Blocks,
                                                       connector =>
                {
                    if (connector.IsFunctional &&
                        connector.DefinitionDisplayNameText == "Connector" &&
                        connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        connector.ApplyAction("Lock");
                        connected = true;
                    }
                });

        if (connected)
        {
            // And 1 second from now, lock landing gear and do everything else
            eventDriver.Schedule(1.0, Docked);
        }
    }

    public void Docked(ZACommons commons, EventDriver eventDriver)
    {
        ZACommons.ForEachBlockOfType<IMyLandingGear>(commons.Blocks,
                                                     gear =>
                {
                    if (gear.IsFunctional && gear.IsWorking &&
                        gear.Enabled && !gear.IsLocked) gear.ApplyAction("Lock");
                });

        // Do everything else needed after docking
        ManageShip(commons, eventDriver, true);
    }

    public void UndockStart(ZACommons commons, EventDriver eventDriver)
    {
        ManageShip(commons, eventDriver, false);

        UndockDetach(commons, eventDriver);
    }

    public void UndockDetach(ZACommons commons, EventDriver eventDriver)
    {
        // Unlock connectors
        var connectors = ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks, connector => connector.DefinitionDisplayNameText == "Connector");
        connectors.ForEach(connector =>
                {
                    if (connector.Status == MyShipConnectorStatus.Connected) connector.ApplyAction("Unlock");
                });

        // Unlock all landing gear
        var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
        gears.ForEach(gear =>
                {
                    if (gear.IsLocked) gear.ApplyAction("Unlock");
                });

        // 1 second from now, disable all connectors
        eventDriver.Schedule(1.0, UndockDisable);
    }

    public void UndockDisable(ZACommons commons, EventDriver eventDriver)
    {
        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks, connector => connector.DefinitionDisplayNameText == "Connector"),
                               false);
    }

    public void ManageShip(ZACommons commons, EventDriver eventDriver,
                           bool docked)
    {
        if (!docked)
        {
            for (var i = 0; i < DockingHandlers.Length; i++)
            {
                DockingHandlers[i].DockingAction(commons, eventDriver, false);
            }
        }

        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyThrust>(commons.Blocks), !docked);
        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyGyro>(commons.Blocks), !docked);
        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(commons.Blocks);
        foreach (var battery in batteries)
        {
            battery.OnlyRecharge = docked;
            battery.OnlyDischarge = !docked;
        }
        if (!docked) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyReactor>(commons.Blocks), true);

        if (TOUCH_ANTENNA)
        {
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks), !docked);
            // Now defaults to off in 01.124. Thanks, Keen!
            if (!docked)
            {
                eventDriver.Schedule(2, (c,ed) =>
                        {
                            ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks).ForEach(antenna => antenna.SetValue<bool>("EnableBroadCast", true));
                        });
            }
        }
        if (TOUCH_LANTENNA) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyLaserAntenna>(commons.Blocks), !docked);
        if (TOUCH_BEACON) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyBeacon>(commons.Blocks), !docked);
        if (TOUCH_LIGHTS) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyLightingBlock>(commons.Blocks), !docked);
        // Disable tools if we just docked
        if (TOUCH_TOOLS && docked) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyShipToolBase>(commons.Blocks), false);
        if (TOUCH_OXYGEN)
        {
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyGasGenerator>(commons.Blocks), !docked);
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyOxygenFarm>(commons.Blocks), !docked);
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyAirVent>(commons.Blocks,
                                                                         vent => ((IMyAirVent)vent).Depressurize &&
                                                                         vent.CustomName.IndexOf("[Intake]", ZACommons.IGNORE_CASE) >= 0), !docked);
            var tanks = ZACommons.GetBlocksOfType<IMyGasTank>(commons.Blocks,
                                                              tank => tank.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0);
            tanks.ForEach(tank => tank.Stockpile = docked);
        }
        if (TOUCH_SENSORS) ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMySensorBlock>(commons.Blocks), !docked);

        if (docked)
        {
            for (var i = 0; i < DockingHandlers.Length; i++)
            {
                DockingHandlers[i].DockingAction(commons, eventDriver, true);
            }
        }

        IsDocked = docked;
        if (IsDocked)
        {
            eventDriver.Schedule(RunDelay, Sleep);
        }
    }

    public void Sleep(ZACommons commons, EventDriver eventDriver)
    {
        if (!IsDocked) return;

        // Just check if we're still connected
        if (!IsConnectedAnywhere(commons.Blocks))
        {
            // Time to panic and/or wake up
            ManageShip(commons, eventDriver, false);
        }
        else
        {
            eventDriver.Schedule(RunDelay, Sleep);
        }
    }

    private static bool IsConnectedAnywhere(IEnumerable<IMyTerminalBlock> connectors)
    {
        foreach (var block in connectors)
        {
            var connector = block as IMyShipConnector;
            if (connector != null && connector.Status == MyShipConnectorStatus.Connected)
            {
                return true;
            }
        }
        return false;
    }
}
