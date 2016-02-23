// If there are no active reactors on the main grid, shut off all reactors
// on all connected grids. Presumably, we are running off battery/solar
// and don't want to drain connected reactors (because power is buggy and
// reactors will charge batteries in the presence of solar).
public class ReactorManager
{
    private const double RunDelay = 5.0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var myReactors = ZACommons.GetBlocksOfType<IMyReactor>(commons.Blocks,
                                                               block => block.IsWorking);
        if (myReactors.Count == 0)
        {
            // Disable reactors on all connected grids
            var reactors = ZACommons.GetBlocksOfType<IMyReactor>(commons.AllBlocks,
                                                                 block => block.CubeGrid != commons.Me.CubeGrid);
            reactors.ForEach(block => block.SetValue<bool>("OnOff", false));
        }

        eventDriver.Schedule(RunDelay, Run);
    }
}
