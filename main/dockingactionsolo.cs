//! Docking Action
//@ commons eventdriver dockingaction
private readonly DockingAction dockingAction = new DockingAction();
private readonly EventDriver eventDriver = new EventDriver();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        dockingAction.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons);
}
