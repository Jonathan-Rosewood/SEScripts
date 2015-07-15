public class SimpleAirlock
{
    private bool IsAnyDoorOpen(List<IMyDoor> doors)
    {
        for (var e = doors.GetEnumerator(); e.MoveNext();)
        {
            if (e.Current.Open) return true;
        }
        return false;
    }

    public void Run(MyGridProgram program)
    {
        var groups = ZALibrary.GetBlockGroupsWithPrefix(program, SIMPLE_AIRLOCK_GROUP_PREFIX);
        for (var e = groups.GetEnumerator(); e.MoveNext();)
        {
            var doors = ZALibrary.GetBlocksOfType<IMyDoor>(e.Current.Blocks, delegate (IMyDoor door)
                                                           {
                                                               return door.CubeGrid == program.Me.CubeGrid &&
                                                               door.IsFunctional;
                                                           });
            var opened = IsAnyDoorOpen(doors);
            for (var f = doors.GetEnumerator(); f.MoveNext();)
            {
                var door = f.Current;
                if (!door.Open && opened)
                {
                    // This door is not open and some other door in the group is, lock it down
                    if (door.Enabled) door.GetActionWithName("OnOff_Off").Apply(door);
                }
                else
                {
                    if (!door.Enabled) door.GetActionWithName("OnOff_On").Apply(door);
                }
            }
        }
    }
}
