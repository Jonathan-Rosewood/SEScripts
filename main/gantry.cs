private readonly EventDriver eventDriver = new EventDriver(timerGroup: "GantryClock");
private readonly PistonStepper udPistonStepper = new PistonStepper("Gantry UpDown", commandPrefix: "gantryy");
private readonly PistonStepper lrPistonStepper = new PistonStepper("Gantry LeftRight", commandPrefix: "gantryx");
private readonly PistonStepper fbPistonStepper = new PistonStepper("Gantry ForwardBackward", commandPrefix: "gantryz");

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        udPistonStepper.Init(commons, eventDriver);
        lrPistonStepper.Init(commons, eventDriver);
        fbPistonStepper.Init(commons, eventDriver);
    }
        
    eventDriver.Tick(commons, preAction: () => {
            udPistonStepper.HandleCommand(commons, eventDriver, argument);
            lrPistonStepper.HandleCommand(commons, eventDriver, argument);
            fbPistonStepper.HandleCommand(commons, eventDriver, argument);
        });
}
