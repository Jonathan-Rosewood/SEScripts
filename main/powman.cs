public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly PowerManager powerManager = new PowerManager();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    eventDriver.Tick(commons, () =>
            {
                powerManager.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
