public class DoorAutoCloser
{
    // Yeah, not sure if it's a good idea to hold references between invocations...
    private readonly Dictionary<IMyDoor, TimeSpan> opened = new Dictionary<IMyDoor, TimeSpan>();

    public void Run(ZACommons commons)
    {
        var doors = ZACommons
            .GetBlocksOfType<IMyDoor>(commons.Blocks,
                                      block => block.IsFunctional &&
                                      block.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0 &&
                                      block.DefinitionDisplayNameText != "Airtight Hangar Door");

        for (var e = doors.GetEnumerator(); e.MoveNext();)
        {
            var door = (IMyDoor)e.Current;

            if (door.Open)
            {
                TimeSpan openTime;
                if (opened.TryGetValue(door, out openTime))
                {
                    openTime += commons.Program.ElapsedTime;
                    if (openTime >= MAX_DOOR_OPEN_TIME)
                    {
                        // Time to close it
                        door.GetActionWithName("Open_Off").Apply(door);
                        opened.Remove(door);
                    }
                    else
                    {
                        opened[door] = openTime;
                    }
                }
                else
                {
                    opened.Add(door, TimeSpan.FromSeconds(0));
                }
            }
            else
            {
                opened.Remove(door);
            }
        }
    }
}
