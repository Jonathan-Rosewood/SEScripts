private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly ComplexAirlock complexAirlock = new ComplexAirlock();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly PowerManager powerManager = new PowerManager();
private readonly RefineryManager refineryManager = new RefineryManager();
private readonly ProductionManager productionManager = new ProductionManager();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    // Handle commands
    if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.HandleCommand(this, eventDriver, argument);
    if (PRODUCTION_MANAGER_ENABLE) productionManager.HandleCommand(argument);

    if (eventDriver.Tick(this))
    {
        // Door management
        if (AUTO_CLOSE_DOORS_ENABLE) doorAutoCloser.Run(this);
        if (SIMPLE_AIRLOCK_ENABLE) simpleAirlock.Run(this);
        if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.Run(this, eventDriver);

        if (OXYGEN_MANAGER_ENABLE || POWER_MANAGER_ENABLE || REFINERY_MANAGER_ENABLE ||
            PRODUCTION_MANAGER_ENABLE)
        {
            // Systems management
            List<IMyTerminalBlock> ship = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(ship);

            if (OXYGEN_MANAGER_ENABLE) oxygenManager.Run(this, ship);
            if (POWER_MANAGER_ENABLE) powerManager.Run(this, ship);
            if (REFINERY_MANAGER_ENABLE) refineryManager.Run(this, ship);
            if (PRODUCTION_MANAGER_ENABLE) productionManager.Run(this, ship);
        }

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
