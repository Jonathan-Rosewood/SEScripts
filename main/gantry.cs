//! Gantry Controller
//@ commons eventdriver pistonstepper
private readonly EventDriver eventDriver = new EventDriver();
private readonly PistonStepper udPistonStepper = new PistonStepper("Gantry UpDown", commandPrefix: "gantryy");
private readonly PistonStepper lrPistonStepper = new PistonStepper("Gantry LeftRight", commandPrefix: "gantryx");
private readonly PistonStepper fbPistonStepper = new PistonStepper("Gantry ForwardBackward", commandPrefix: "gantryz");

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType);

    if (FirstRun)
    {
        FirstRun = false;
        udPistonStepper.Init(commons, eventDriver);
        lrPistonStepper.Init(commons, eventDriver);
        fbPistonStepper.Init(commons, eventDriver);
    }
        
    eventDriver.Tick(commons, argAction: () => {
            udPistonStepper.HandleCommand(commons, eventDriver, argument);
            lrPistonStepper.HandleCommand(commons, eventDriver, argument);
            fbPistonStepper.HandleCommand(commons, eventDriver, argument);
        });
}
