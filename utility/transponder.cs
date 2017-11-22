//@ commons eventdriver customdata
public class Transponder
{
    private const double RunDelay = 3.0; // For cleanup task

    private string TransponderID;

    public struct TransponderInfo
    {
        public string ID; // Redundant, but useful
        public Vector3D Position;
        public QuaternionD Orientation;
        public TimeSpan ExpireTime;

        public TransponderInfo(string ID, Vector3D position, QuaternionD orientation, TimeSpan expireTime)
        {
            this.ID = ID;
            Position = position;
            Orientation = orientation;
            ExpireTime = expireTime;
        }
    }

    private readonly TimeSpan ExpireTimeout = TimeSpan.Parse(TRANSPONDER_TIMEOUT);

    private readonly Dictionary<string, TransponderInfo> ReceivedInfos = new Dictionary<string, TransponderInfo>();

    public void Init(ZACommons commons, EventDriver eventDriver, ZACustomData customData)
    {
        TransponderID = customData.GetValue("transponderID", "default");
        if (!TransponderID.Equals("default"))
        {
            eventDriver.Schedule(0, Run);
        }
        eventDriver.Schedule(RunDelay, Cleanup);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim();
        var parts = argument.Split(new char[] { ';' }, 9);
        if (parts.Length < 9) return;
        var command = parts[0];

        if (command == "xponder")
        {
            var id = parts[1];
            double posX, posY, posZ;
            if (!double.TryParse(parts[2], out posX) ||
                !double.TryParse(parts[3], out posY) ||
                !double.TryParse(parts[4], out posZ)) return;
            double orientX, orientY, orientZ, orientW;
            if (!double.TryParse(parts[5], out orientX) ||
                !double.TryParse(parts[6], out orientY) ||
                !double.TryParse(parts[7], out orientZ) ||
                !double.TryParse(parts[8], out orientW)) return;

            var info = new TransponderInfo(id,
                                           new Vector3D(posX, posY, posZ),
                                           new QuaternionD(orientX, orientY, orientZ, orientW),
                                           eventDriver.TimeSinceStart + ExpireTimeout);
            ReceivedInfos[id] = info;
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = commons as ShipControlCommons;
        var position = shipControl.ReferencePoint;
        var orientation = QuaternionD.CreateFromForwardUp(shipControl.ReferenceForward, shipControl.ReferenceUp);

        var msg = string.Format("xponder;{0};{1};{2};{3};{4};{5};{6};{7}",
                                TransponderID,
                                position.X, position.Y, position.Z,
                                orientation.X, orientation.Y, orientation.Z,
                                orientation.W);

        // Transmit on first functional antenna
        var antennas = ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks, antenna => antenna.IsFunctional && antenna.Enabled);
        if (antennas.Count > 0)
        {
            var antenna = antennas[0];
            antenna.TransmitMessage(msg, TRANSPONDER_TARGET);
        }

        eventDriver.Schedule(TRANSPONDER_UPDATE_RATE, Run);
    }

    public void Cleanup(ZACommons commons, EventDriver eventDriver)
    {
        // Clean up received pings
        var toDelete = ReceivedInfos.Where((kv, index) => kv.Value.ExpireTime <= eventDriver.TimeSinceStart).ToArray();
        foreach (var kv in toDelete)
        {
            ReceivedInfos.Remove(kv.Key);
        }

        eventDriver.Schedule(RunDelay, Cleanup);
    }

    public bool TryGetTransponderInfo(string ID, out TransponderInfo info)
    {
        return ReceivedInfos.TryGetValue(ID, out info);
    }

    public Dictionary<string, TransponderInfo> GetTransponderInfos()
    {
        // Better way to do this?
        var result = new Dictionary<string, TransponderInfo>();
        foreach (var kv in ReceivedInfos) result[kv.Key] = kv.Value;
        return result;
    }
}
