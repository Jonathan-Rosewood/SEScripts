public class ComplexAirlock
{
    public class OpenQueueEntry
    {
        public int DesiredState { get; private set; }
        public HashSet<IMyDoor> Doors { get; private set; }

        public OpenQueueEntry(int desiredState, HashSet<IMyDoor> doors)
        {
            DesiredState = desiredState;
            Doors = doors;
        }
    }

    // Enums broken?
    private const int AIRLOCK_STATE_VACUUM = 0;
    private const int AIRLOCK_STATE_PRESSURIZED = 1;
    private const int AIRLOCK_STATE_UNKNOWN = -1;

    private readonly List<IMyBlockGroup> rooms = new List<IMyBlockGroup>();
    private readonly Dictionary<string, IMyBlockGroup> roomsMap = new Dictionary<string, IMyBlockGroup>();

    private readonly HashSet<IMyDoor> innerDoors = new HashSet<IMyDoor>();
    private readonly HashSet<IMyDoor> spaceDoors = new HashSet<IMyDoor>();

    private readonly Dictionary<string, IMyBlockGroup> doorVentGroups = new Dictionary<string, IMyBlockGroup>();
    private readonly Dictionary<IMyDoor, IMyBlockGroup> doorVentRooms = new Dictionary<IMyDoor, IMyBlockGroup>(); // Reverse mapping of rooms
    private readonly Dictionary<IMyDoor, List<IMyAirVent>> doorVentMap = new Dictionary<IMyDoor, List<IMyAirVent>>();

    private readonly Dictionary<string, OpenQueueEntry> openQueue = new Dictionary<string, OpenQueueEntry>();

    private void Init(MyGridProgram program)
    {
        var groups = ZALibrary.GetBlockGroupsWithPrefix(program, "Airlock");
        // Classify each group
        for (var e = groups.GetEnumerator(); e.MoveNext();)
        {
            var group = e.Current;
            if (string.Equals("AirlockDoorInner", group.Name, ZALibrary.IGNORE_CASE))
            {
                innerDoors.UnionWith(ZALibrary.GetBlocksOfType<IMyDoor>(group.Blocks));
            }
            else if (string.Equals("AirlockDoorSpace", group.Name, ZALibrary.IGNORE_CASE))
            {
                spaceDoors.UnionWith(ZALibrary.GetBlocksOfType<IMyDoor>(group.Blocks));
            }
            else if (group.Name.StartsWith("AirlockDoor", ZALibrary.IGNORE_CASE))
            {
                doorVentGroups.Add(group.Name, group);

                var vents = ZALibrary.GetBlocksOfType<IMyAirVent>(group.Blocks);
                var doors = ZALibrary.GetBlocksOfType<IMyDoor>(group.Blocks);
                for (var f = doors.GetEnumerator(); f.MoveNext();)
                {
                    doorVentMap.Add(f.Current, vents);
                }
            }
            else
            {
                rooms.Add(group);

                roomsMap.Add(group.Name, group);

                var doors = ZALibrary.GetBlocksOfType<IMyDoor>(group.Blocks);
                for (var f = doors.GetEnumerator(); f.MoveNext();)
                {
                    doorVentRooms.Add(f.Current, group);
                }
            }
        }
    }

    private void Clear()
    {
        rooms.Clear();
        roomsMap.Clear();
        innerDoors.Clear();
        spaceDoors.Clear();
        doorVentGroups.Clear();
        doorVentRooms.Clear();
        doorVentMap.Clear();
    }

    private int GetAirlockState(List<IMyAirVent> vents)
    {
        if (vents.Count == 0) return AIRLOCK_STATE_UNKNOWN;

        float level = 0.0f;
        for (var e = vents.GetEnumerator(); e.MoveNext();)
        {
            level += e.Current.GetOxygenLevel();
        }
        level /= vents.Count;

        if (level == 0.0f) return AIRLOCK_STATE_VACUUM;
        else if (level > 0.5f) return AIRLOCK_STATE_PRESSURIZED;
        else { return AIRLOCK_STATE_UNKNOWN; }
    }

    private void DepressurizeVents(IEnumerable<IMyAirVent> vents, bool depressurize)
    {
        var e = vents.GetEnumerator();
        while (e.MoveNext())
        {
            var vent = e.Current;
            vent.GetActionWithName(depressurize ? "Depressurize_On" : "Depressurize_Off").Apply(vent);
        }
    }

    private void OpenCloseDoors(IEnumerable<IMyDoor> doors, bool open)
    {
        var e = doors.GetEnumerator();
        while (e.MoveNext())
        {
            var door = e.Current;
            door.GetActionWithName(open ? "Open_On" : "Open_Off").Apply(door);
        }
    }

    private void ChangeRoomState(string roomName,
                                 List<IMyAirVent> vents, List<IMyDoor> doors,
                                 int current, int target,
                                 IEnumerable<IMyDoor> targetDoors = null)
    {
        if (target != current && target != AIRLOCK_STATE_UNKNOWN)
        {
            OpenCloseDoors(doors, false);
            DepressurizeVents(vents, target == AIRLOCK_STATE_VACUUM);

        }

        // Open doors regardless
        var entry = new OpenQueueEntry(target,
                                       targetDoors != null ?
                                       new HashSet<IMyDoor>(targetDoors) :
                                       new HashSet<IMyDoor>());
        openQueue[roomName] = entry;
    }

    private void HandleCommandInternal(string argument)
    {
        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length != 2) return;
        var command = parts[0];
        argument = parts[1].Trim();

        if (command == "inner" || command == "space" || command == "toggle")
        {
            IMyBlockGroup room;
            if (roomsMap.TryGetValue(argument, out room))
            {
                var vents = ZALibrary.GetBlocksOfType<IMyAirVent>(room.Blocks);
                var current = GetAirlockState(vents);
                int target = AIRLOCK_STATE_UNKNOWN;
                switch (command)
                {
                    case "space":
                        target = AIRLOCK_STATE_VACUUM;
                        break;
                    case "inner":
                        target = AIRLOCK_STATE_PRESSURIZED;
                        break;
                    case "toggle":
                        target = current == AIRLOCK_STATE_PRESSURIZED ?
                            AIRLOCK_STATE_VACUUM : AIRLOCK_STATE_PRESSURIZED;
                        break;
                }

                ChangeRoomState(room.Name,
                                vents, ZALibrary.GetBlocksOfType<IMyDoor>(room.Blocks),
                                current, target, null);
            }
        }
        else if (command == "open")
        {
            // Find named group
            IMyBlockGroup group;
            if (doorVentGroups.TryGetValue(argument, out group))
            {
                var doors = ZALibrary.GetBlocksOfType<IMyDoor>(group.Blocks);
                // Only need to do one (assumes door groups were set up correctly... heh)
                if (doors.Count > 0)
                {
                    var door = doors[0];
                    IMyBlockGroup room;
                    if (doorVentRooms.TryGetValue(door, out room))
                    {
                        var otherVents = ZALibrary.GetBlocksOfType<IMyAirVent>(group.Blocks);
                        var roomVents = ZALibrary.GetBlocksOfType<IMyAirVent>(room.Blocks);
                        var target = GetAirlockState(otherVents);
                        var current = GetAirlockState(roomVents);

                        ChangeRoomState(room.Name,
                                        roomVents,
                                        ZALibrary.GetBlocksOfType<IMyDoor>(room.Blocks),
                                        current, target, doors);
                    }
                }
            }
        }
    }

    private void CloseDoorsAsNeeded(EventDriver eventDriver,
                                    IMyBlockGroup room, List<IMyDoor> doors,
                                    HashSet<IMyDoor> targetDoors,
                                    int checkState)
    {
        var openDoors = new HashSet<IMyDoor>();
        OpenQueueEntry entry;
        if (openQueue.TryGetValue(room.Name, out entry))
        {
            if (entry.DesiredState == checkState)
            {
                openQueue.Remove(room.Name);
                openDoors = entry.Doors;
                if (openDoors.Count == 0)
                {
                    openDoors = new HashSet<IMyDoor>(targetDoors); // NB copy
                }

                // Limit to just doors in this room
                openDoors.IntersectWith(doors);
            }
        }

        // Close and lock all doors with different pressure
        for (var f = doors.GetEnumerator(); f.MoveNext();)
        {
            var door = f.Current;

            int otherState;
            List<IMyAirVent> otherVents;
            if (doorVentMap.TryGetValue(door, out otherVents))
            {
                otherState = GetAirlockState(otherVents);
            }
            else { otherState = AIRLOCK_STATE_UNKNOWN; }

            if (targetDoors.Contains(door) || otherState == checkState)
            {
                // Unlock
                if (!door.Enabled)
                {
                    door.GetActionWithName("OnOff_On").Apply(door);
                }
            }
            else
            {
                // Close & lock all others
                if (door.Open)
                {
                    door.GetActionWithName("Open_Off").Apply(door);
                }
                else if (door.OpenRatio == 0.0f && door.Enabled)
                {
                    door.GetActionWithName("OnOff_Off").Apply(door);
                }
            }
        }

        // Open all required doors at next tick
        if (openDoors.Count > 0)
        {
            eventDriver.Schedule(2.5, (p, e) =>
                    {
                        for (var f = openDoors.GetEnumerator(); f.MoveNext();)
                        {
                            var door = f.Current;
                            door.GetActionWithName("Open_On").Apply(door);
                        }
                    });
        }
    }

    private void OpenCloseDoorsAsNeeded(EventDriver eventDriver)
    {
        for (var e = rooms.GetEnumerator(); e.MoveNext();)
        {
            var room = e.Current;
            var vents = ZALibrary.GetBlocksOfType<IMyAirVent>(room.Blocks);
            if (vents.Count == 0) continue;

            var doors = ZALibrary.GetBlocksOfType<IMyDoor>(room.Blocks);
            if (doors.Count == 0) continue;

            // Determine room state
            var state = GetAirlockState(vents);

            switch (state)
            {
                case AIRLOCK_STATE_VACUUM:
                    // Close and lock all but space doors
                    CloseDoorsAsNeeded(eventDriver, room, doors, spaceDoors,
                                       AIRLOCK_STATE_VACUUM);
                    break;
                case AIRLOCK_STATE_PRESSURIZED:
                    // Close and lock all but inner doors
                    CloseDoorsAsNeeded(eventDriver, room, doors, innerDoors,
                                       AIRLOCK_STATE_PRESSURIZED);
                    break;
                case AIRLOCK_STATE_UNKNOWN:
                    // Close and lock all doors
                    for (var f = doors.GetEnumerator(); f.MoveNext();)
                    {
                        var door = f.Current;
                        door.GetActionWithName("Open_Off").Apply(door);
                        if (door.OpenRatio == 0.0f && door.Enabled)
                        {
                            door.GetActionWithName("OnOff_Off").Apply(door);
                        }
                    }
                    break;
            }
        }
    }

    public void Run(MyGridProgram program, EventDriver eventDriver, string argument)
    {
        Init(program);

        if (!string.IsNullOrWhiteSpace(argument))
        {
            HandleCommandInternal(argument);
        }

        OpenCloseDoorsAsNeeded(eventDriver);

        Clear();
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        Run(program, eventDriver, ""); // Why...
    }

    public void HandleCommand(MyGridProgram program, EventDriver eventDriver, string argument)
    {
        Run(program, eventDriver, argument); // Why...
    }
}
