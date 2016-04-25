//@ commons eventdriver
public class DoorAutoCloser
{
    private const double RunDelay = 1.0;
    private const char DURATION_DELIMITER = ':';

    // Yeah, not sure if it's a good idea to hold references between invocations...
    private readonly Dictionary<IMyDoor, TimeSpan> opened = new Dictionary<IMyDoor, TimeSpan>();

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var groups = commons.GetBlockGroupsWithPrefix(DOOR_AUTO_CLOSER_PREFIX);
        if (groups.Count > 0)
        {
            groups.ForEach(group => {
                    // Determine open duration
                    var parts = group.Name.Split(new char[] { DURATION_DELIMITER }, 2);
                    var duration = DEFAULT_DOOR_OPEN_DURATION;
                    if (parts.Length == 2)
                    {
                        if (!double.TryParse(parts[1], out duration))
                        {
                            duration = DEFAULT_DOOR_OPEN_DURATION;
                        }
                    }

                    var doors = ZACommons.GetBlocksOfType<IMyDoor>(group.Blocks,
                                                                   block => block.IsFunctional);
                    CloseDoors(commons, eventDriver, doors, duration);
                });
        }
        else
        {
            // Default behavior (all doors except vanilla Airtight Hangar Doors and tagged doors)
            var doors = ZACommons
                .GetBlocksOfType<IMyDoor>(commons.Blocks,
                                          block => block.IsFunctional &&
                                          block.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0 &&
                                          block.DefinitionDisplayNameText != "Airtight Hangar Door");
            CloseDoors(commons, eventDriver, doors, DEFAULT_DOOR_OPEN_DURATION);
        }
        eventDriver.Schedule(RunDelay, Run);
    }

    private void CloseDoors(ZACommons commons, EventDriver eventDriver, List<IMyTerminalBlock> doors,
                            double openDurationSeconds)
    {
        var openDuration = TimeSpan.FromSeconds(openDurationSeconds);

        doors.ForEach(block => {
                var door = (IMyDoor)block;

                if (door.Open)
                {
                    TimeSpan closeTime;
                    if (opened.TryGetValue(door, out closeTime))
                    {
                        if (closeTime <= eventDriver.TimeSinceStart)
                        {
                            // Time to close it
                            door.SetValue<bool>("Open", false);
                            opened.Remove(door);
                        }
                    }
                    else
                    {
                        opened.Add(door, eventDriver.TimeSinceStart + openDuration);
                    }
                }
                else
                {
                    opened.Remove(door);
                }
            });
    }
}
