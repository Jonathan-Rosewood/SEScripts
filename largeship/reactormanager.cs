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
}