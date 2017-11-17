//! Turret Detector
//@ commons eventdriver turretdetector
public readonly EventDriver eventDriver = new EventDriver();
public readonly TurretBasedDetector turretDetector = new TurretBasedDetector();

private bool FirstRun = true;

void Main(string argument)
{
    ZACommons commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;

        turretDetector.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            turretDetector.HandleCommand(commons, eventDriver, argument);
        });
}
