//! Turret Detector
//@ commons eventdriver turretdetector
public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
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
