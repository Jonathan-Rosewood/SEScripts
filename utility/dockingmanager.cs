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
        var docked = ZACommons.IsConnectedAnywhere(commons.Blocks);
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
                                                       block =>
                {
                    var connector = (IMyShipConnector)block;
                    if (connector.IsFunctional &&
                        connector.DefinitionDisplayNameText == "Connector")
                    {
                        connector.SetValue<bool>("OnOff", true);
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
                                                       block =>
                {
                    var connector = (IMyShipConnector)block;
                    if (connector.IsFunctional &&
                        connector.DefinitionDisplayNameText == "Connector" &&
                        connector.IsLocked && !connector.IsConnected)
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
                                                     block =>
                {
                    var gear = (IMyLandingGear)block;
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
        connectors.ForEach(block =>
                {
                    var connector = (IMyShipConnector)block;
                    if (connector.IsLocked && connector.IsConnected) connector.ApplyAction("Unlock");
                });

        // Unlock all landing gear
        var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
        gears.ForEach(block =>
                {
                    var gear = (IMyLandingGear)block;
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
        for (var e = batteries.GetEnumerator(); e.MoveNext();)
        {
            var battery = (IMyBatteryBlock)e.Current;
            battery.SetValue<bool>("Recharge", docked);
            battery.SetValue<bool>("Discharge", !docked);
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
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyOxygenGenerator>(commons.Blocks), !docked);
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyOxygenFarm>(commons.Blocks), !docked);
            ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyAirVent>(commons.Blocks,
                                                                         vent => ((IMyAirVent)vent).IsDepressurizing &&
                                                                         vent.CustomName.IndexOf("[Intake]", ZACommons.IGNORE_CASE) >= 0), !docked);
            var tanks = ZACommons.GetBlocksOfType<IMyOxygenTank>(commons.Blocks,
                                                                 tank => tank.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0);
            tanks.ForEach(tank => tank.SetValue<bool>("Stockpile", docked));
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
        if (!ZACommons.IsConnectedAnywhere(commons.Blocks))
        {
            // Time to panic and/or wake up
            ManageShip(commons, eventDriver, false);
        }
        else
        {
            eventDriver.Schedule(RunDelay, Sleep);
        }
    }
}
