public class DoorAutoCloser
{
    // Yeah, not sure if it's a good idea to hold references between invocations...
    private readonly Dictionary<IMyDoor, DateTime> opened = new Dictionary<IMyDoor, DateTime>();

    public void Run(ZACommons commons)
    {
        var doors = ZACommons
            .GetBlocksOfType<IMyDoor>(commons.Blocks,
                                      block => block.IsFunctional &&
                                      block.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0 &&
                                      block.DefinitionDisplayNameText != "Airtight Hangar Door");

        var closeTime = commons.Now - MAX_DOOR_OPEN_TIME;
        for (var e = doors.GetEnumerator(); e.MoveNext();)
        {
            var door = (IMyDoor)e.Current;

            if (door.Open)
            {
                DateTime openTime;
                if (opened.TryGetValue(door, out openTime))
                {
                    if (openTime <= closeTime)
                    {
                        // Time to close it
                        door.GetActionWithName("Open_Off").Apply(door);
                        opened.Remove(door);
                    }
                }
                else
                {
                    opened.Add(door, commons.Now);
                }
            }
            else
            {
                opened.Remove(door);
            }
        }
    }
}
