//@ commons eventdriver
public class TimerKicker
{
    private const double RunDelay = 5.0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        Run(commons);
        eventDriver.Schedule(RunDelay, Run);
    }

    public void Run(ZACommons commons)
    {
        var timers = ZACommons.SearchBlocksOfName(commons.AllBlocks, STANDARD_LOOP_TIMER_BLOCK_NAME,
                                                  block => block is IMyTimerBlock &&
                                                  block.IsFunctional &&
                                                  block.IsWorking &&
                                                  ((IMyTimerBlock)block).Enabled &&
                                                  !((IMyTimerBlock)block).IsCountingDown);
        timers.ForEach(timer => timer.ApplyAction("Start"));
    }
}
