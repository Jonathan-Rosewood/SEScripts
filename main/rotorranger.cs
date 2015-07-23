private readonly EventDriver eventDriver = new EventDriver();
private readonly RotorRangefinder rotorRangefinder = new RotorRangefinder();

private bool FirstRun = true;

public void UpdateTargetTextPanels(Vector3D target)
{
    var targetGroup = ZALibrary.GetBlockGroupWithName(this, RANGEFINDER_TARGET_GROUP);
    if (targetGroup != null)
    {
        var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                         target.GetDim(0),
                                         target.GetDim(1),
                                         target.GetDim(2));

        for (var e = ZALibrary.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
        {
            e.Current.WritePublicText(targetString);
        }
    }
}

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        rotorRangefinder.Init(this, eventDriver);
    }
        
    eventDriver.Tick(this);

    rotorRangefinder.HandleCommand(this, eventDriver, argument,
                                   UpdateTargetTextPanels);
}
