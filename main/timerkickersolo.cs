private readonly EventDriver eventDriver = new EventDriver(timerName: TIMER_KICKER_CLOCK_NAME);
private readonly TimerKicker timerKicker = new TimerKicker();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        timerKicker.Init(this, eventDriver);
    }

    eventDriver.Tick(this);
}
