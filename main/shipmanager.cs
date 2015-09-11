public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
public readonly ComplexAirlock complexAirlock = new ComplexAirlock();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly PowerManager powerManager = new PowerManager();
private readonly RefineryManager refineryManager = new RefineryManager();
public readonly ProductionManager productionManager = new ProductionManager();
private readonly TimerKicker timerKicker = new TimerKicker();
private readonly RedundancyManager redundancyManager = new RedundancyManager();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0, Run);
        if (TIMER_KICKER_ENABLE) timerKicker.Init(commons, eventDriver);
        if (REDUNDANCY_MANAGER_ENABLE) redundancyManager.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            // Handle commands
            if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.HandleCommand(commons, eventDriver, argument);
            if (PRODUCTION_MANAGER_ENABLE) productionManager.HandleCommand(argument);
        });
}

public void Run(ZACommons commons, EventDriver eventDriver)
{
    // Door management
    if (AUTO_CLOSE_DOORS_ENABLE) doorAutoCloser.Run(commons);
    if (SIMPLE_AIRLOCK_ENABLE) simpleAirlock.Run(commons);
    if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.Run(commons, eventDriver);

    // Systems management
    if (OXYGEN_MANAGER_ENABLE) oxygenManager.Run(commons);
    if (POWER_MANAGER_ENABLE) powerManager.Run(commons);
    if (REFINERY_MANAGER_ENABLE) refineryManager.Run(commons);
    if (PRODUCTION_MANAGER_ENABLE) productionManager.Run(commons);

    eventDriver.Schedule(1.0, Run);
}
