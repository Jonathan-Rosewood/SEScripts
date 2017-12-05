//! Rover Controller
//@ commons eventdriver dockingmanager safemode damagecontrol
//@ batterymonitor redundancy
private readonly EventDriver eventDriver = new EventDriver();
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly DamageControl damageControl = new DamageControl();
private readonly ZAStorage myStorage = new ZAStorage();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType,
                                shipGroup: SHIP_GROUP,
                                storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager(),
                            new ManageRover());
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

public class ManageRover : DockingHandler
{
    public void PreDock(ZACommons commons, EventDriver eventDriver)
    {
        // Lower suspension strength all the way
        var wheels = ZACommons.GetBlocksOfType<IMyMotorSuspension>(commons.Blocks);
        wheels.ForEach(wheel =>
                {
                    wheel.SetValue<float>("Strength", wheel.GetMinimum<float>("Strength"));
                });
    }

    public void DockingAction(ZACommons commons, EventDriver eventDriver,
                               bool docked)
    {
        var wheels = ZACommons.GetBlocksOfType<IMyMotorSuspension>(commons.Blocks);
        ZACommons.EnableBlocks(wheels, !docked);
        if (!docked)
        {
            // Set suspension strength to configured value
            wheels.ForEach(wheel =>
                    {
                        wheel.SetValue<float>("Strength", UNDOCK_SUSPENSION_STRENGTH);
                    });
        }
        else
        {
            // Apply handbrake
            var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks);
            controllers.ForEach(controller => controller.HandBrake = true);
        }
    }
}
