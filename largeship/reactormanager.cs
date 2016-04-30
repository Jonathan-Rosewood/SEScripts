//@ commons eventdriver
// If there are no active reactors on the main grid, shut off all reactors
// on all connected grids. Presumably, we are running off battery/solar
// and don't want to drain connected reactors (because power is buggy and
// reactors will charge batteries in the presence of solar).
public class ReactorManager
{
    private const double RunDelay = 5.0;

    private bool? State = null;
    private int ConnectorCount = 0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        State = null;
        ConnectorCount = 0;
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var myConnectors = ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks,
                                                                       block => block.DefinitionDisplayNameText == "Connector" &&
                                                                       ((IMyShipConnector)block).IsLocked &&
                                                                       ((IMyShipConnector)block).IsConnected);
        var currentConnectorCount = myConnectors.Count;
        if (currentConnectorCount > ConnectorCount)
        {
            // New connection, force re-evaluation
            State = null;
        }
        ConnectorCount = currentConnectorCount;

        var myReactors = ZACommons.GetBlocksOfType<IMyReactor>(commons.Blocks,
                                                               block => block.IsWorking);
        var currentState = myReactors.Count > 0;

        // Only on state change
        if (State == null || currentState != (bool)State)
        {
            State = currentState;

            if (!(bool)State)
            {
                // Disable reactors on all connected grids
                var reactors = ZACommons.GetBlocksOfType<IMyReactor>(commons.AllBlocks,
                                                                     block => block.CubeGrid != commons.Me.CubeGrid);
                reactors.ForEach(block => block.SetValue<bool>("OnOff", false));
            }
        }

        eventDriver.Schedule(RunDelay, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        switch (command)
        {
            case "reactors":
                {
                    // Turn on all reactors
                    GetAllReactors(commons).ForEach(block => block.SetValue<bool>("OnOff", true));
                    eventDriver.Schedule(1.0, (c,ed) => {
                            // Turn off all local batteries
                            GetBatteries(c).ForEach(block => block.SetValue<bool>("OnOff", false));
                        });
                    break;
                }
            case "batteries":
                {
                    // Turn on all local batteries
                    // and disable recharge/discharge
                    GetBatteries(commons).ForEach(block =>
                            {
                                block.SetValue<bool>("OnOff", true);
                                block.SetValue<bool>("Recharge", false);
                                block.SetValue<bool>("Discharge", false);
                            });
                    eventDriver.Schedule(1.0, (c,ed) => {
                            // Turn off all reactors
                            GetAllReactors(c).ForEach(block => block.SetValue<bool>("OnOff", false));
                        });
                    break;
                }
        }
    }

    private static List<IMyTerminalBlock> GetAllReactors(ZACommons commons)
    {
        return ZACommons.GetBlocksOfType<IMyReactor>(commons.AllBlocks,
                                                     reactor => reactor.IsFunctional &&
                                                     reactor.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0);
    }

    private static List<IMyTerminalBlock> GetBatteries(ZACommons commons)
    {
        return ZACommons.GetBlocksOfType<IMyBatteryBlock>(commons.Blocks,
                                                          battery => battery.IsFunctional &&
                                                          battery.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0);
    }
}
