public class TimerKicker
{
    private const double RunDelay = 5.0;

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        Run(program);
        eventDriver.Schedule(RunDelay, Run);
    }

    public void Run(MyGridProgram program)
    {
        var timers = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.SearchBlocksOfName(ZALIBRARY_LOOP_TIMER_BLOCK_NAME, timers,
                                                      block => block is IMyTimerBlock &&
                                                      block.IsFunctional &&
                                                      block.IsWorking &&
                                                      ((IMyTimerBlock)block).Enabled &&
                                                      !((IMyTimerBlock)block).IsCountingDown);
        timers.ForEach(timer => timer.GetActionWithName("Start").Apply(timer));
    }
}
