//! Turret Detector
//@ commons eventdriver turretdetector
public readonly EventDriver eventDriver = new EventDriver();
public readonly TurretBasedDetector turretDetector = new TurretBasedDetector();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    ZACommons commons = new ZACommons(this, updateType);

    if (FirstRun)
    {
        FirstRun = false;

        turretDetector.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            turretDetector.HandleCommand(commons, eventDriver, argument);
        });
}
