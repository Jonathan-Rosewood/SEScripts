//! Rotor Ranger
//@ commons eventdriver rotorrangefinder
public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME, timerGroup: "RotorRangerClock");
public readonly RotorRangefinder rotorRangefinder = new RotorRangefinder();

private bool FirstRun = true;

public void UpdateTargetTextPanels(ZACommons commons, Vector3D target)
{
    var targetGroup = commons.GetBlockGroupWithName(RANGEFINDER_TARGET_GROUP);
    if (targetGroup != null)
    {
        var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                         target.GetDim(0),
                                         target.GetDim(1),
                                         target.GetDim(2));

        foreach (var panel in ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks))
        {
            panel.WritePublicText(targetString);
        }
    }

    // Also output to terminal
    commons.Echo(string.Format("Target: {0:F2}, {1:F2}, {2:F2}",
                               target.GetDim(0),
                               target.GetDim(1),
                               target.GetDim(2)));
    var distance = (target - commons.Me.GetPosition()).Length();
    commons.Echo(string.Format("Distance: {0:F2} m", distance));
}

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        rotorRangefinder.Init(commons, eventDriver);
    }
        
    eventDriver.Tick(commons, preAction: () => {
            rotorRangefinder.HandleCommand(commons, eventDriver, argument,
                                           UpdateTargetTextPanels);
        });
}
