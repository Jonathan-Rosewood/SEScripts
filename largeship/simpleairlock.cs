//@ commons eventdriver
public class SimpleAirlock
{
    private const double RunDelay = 1.0;

    private bool IsAnyDoorOpen(List<IMyDoor> doors)
    {
        foreach (var door in doors)
        {
            if (door.OpenRatio > 0.0f) return true;
        }
        return false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var groups = commons.GetBlockGroupsWithPrefix(SIMPLE_AIRLOCK_GROUP_PREFIX);
        foreach (var group in groups)
        {
            var doors = ZACommons.GetBlocksOfType<IMyDoor>(group.Blocks,
                                                           door => door.CubeGrid == commons.Me.CubeGrid &&
                                                           door.IsFunctional);

            var opened = IsAnyDoorOpen(doors);
            foreach (var door in doors)
            {
                if (door.OpenRatio == 0.0f && opened)
                {
                    // This door is not open and some other door in the group is, lock it down
                    if (door.Enabled) door.SetValue<bool>("OnOff", false);
                }
                else
                {
                    if (!door.Enabled) door.SetValue<bool>("OnOff", true);
                }
            }
        }

        eventDriver.Schedule(RunDelay, Run);
    }
}
