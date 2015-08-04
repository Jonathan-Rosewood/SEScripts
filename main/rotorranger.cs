private readonly EventDriver eventDriver = new EventDriver();
private readonly RotorRangefinder rotorRangefinder = new RotorRangefinder();

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

        for (var e = ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
        {
            e.Current.WritePublicText(targetString);
        }
    }
}

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        rotorRangefinder.Init(commons, eventDriver);
    }
        
    rotorRangefinder.HandleCommand(commons, eventDriver, argument,
                                   UpdateTargetTextPanels);

    eventDriver.Tick(commons);
}
