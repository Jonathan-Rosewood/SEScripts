//@ commons
public static class TimerBlockUtils
{
    public static bool StartTimerBlockWithName(IEnumerable<IMyTerminalBlock> blocks, string name,
                                               Func<IMyTimerBlock, bool> condition = null)
    {
        var timer = ZACommons.GetBlockWithName<IMyTimerBlock>(blocks, name);
        if (timer != null && timer.Enabled && !timer.IsCountingDown &&
            (condition == null || condition(timer)))
        {
            timer.StartCountdown();
            return true;
        }
        return false;
    }

}
