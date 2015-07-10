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
        var groups = ZALibrary.GetBlockGroupsWithPrefix(program, "SimpleAirlock");
        for (var e = groups.GetEnumerator(); e.MoveNext();)
        {
            var doors = ZALibrary.GetBlocksOfType<IMyDoor>(e.Current.Blocks);
            var opened = IsAnyDoorOpen(doors);
            for (var f = doors.GetEnumerator(); f.MoveNext();)
            {
                var door = f.Current;
                if (!door.Open && opened)
                {
                    // This door is not open and some other door in the group is, lock it down
                    door.GetActionWithName("OnOff_Off").Apply(door);
                }
                else
                {
                    door.GetActionWithName("OnOff_On").Apply(door);
                }
            }
        }
    }
}
