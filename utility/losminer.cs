public class LOSMiner
{
    private const string LOSMinerKey = "LOSMiner_Data";

    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.0);

    private const double LOS_OFFSET = 200.0; // Meters forward

    private Vector3D StartPoint;
    private Vector3D StartDirection, StartUp, StartLeft;

    // NB Saved in Storage. Do not change.
    private const int IDLE = 0;
    private const int MINING = 1;
    private const int REVERSING = 2;

    private int Mode = IDLE;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var previous = commons.GetValue(LOSMinerKey);
        if (previous != null)
        {
            var parts = previous.Split(';');
            if (parts.Length == 13)
            {
                // Resume original mode and line-of-sight vector
                var newMode = int.Parse(parts[0]);
                StartPoint = new Vector3D();
                StartDirection = new Vector3D();
                StartUp = new Vector3D();
                StartLeft = new Vector3D();
                for (int i = 0; i < 3; i++)
                {
                    StartPoint.SetDim(i, double.Parse(parts[i+1]));
                    StartDirection.SetDim(i, double.Parse(parts[i+4]));
                    StartUp.SetDim(i, double.Parse(parts[i+7]));
                    StartLeft.SetDim(i, double.Parse(parts[i+10]));
                }

                if (newMode == MINING)
                {
                    Start((ShipControlCommons)commons, eventDriver);
                }
                else if (newMode == REVERSING)
                {
                    StartReverse((ShipControlCommons)commons, eventDriver);
                }
            }
        }
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        switch (command)
        {
            case "start":
                {
                    var shipControl = (ShipControlCommons)commons;
                    SetTarget(shipControl);
                    Start(shipControl, eventDriver);
                    SaveTarget(shipControl);
                }
                break;
            case "reverse":
                {
                    var shipControl = (ShipControlCommons)commons;
                    SetTarget(shipControl);
                    StartReverse(shipControl, eventDriver);
                    SaveTarget(shipControl);
                }
                break;
            case "stop":
                {
                    var shipControl = (ShipControlCommons)commons;
                    shipControl.Reset(gyroOverride: false);

                    Mode = IDLE;
                    ForgetTarget(shipControl);
                }
                break;
        }
    }

    private void Start(ShipControlCommons shipControl, EventDriver eventDriver)
    {
        shipControl.Reset(gyroOverride: true);

        if (MINING_ROLL_RPM > 0.0f) shipControl.GyroControl.SetAxisVelocityRPM(GyroControl.Roll, MINING_ROLL_RPM);

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);
        cruiser.Init(shipControl,
                     localForward: Base6Directions.Direction.Forward);

        if (Mode != MINING)
        {
            Mode = MINING;
            eventDriver.Schedule(0, Mine);
        }
    }

    public void Mine(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != MINING) return;

        var shipControl = (ShipControlCommons)commons;

        var targetVector = GetTarget(shipControl, StartDirection);
        targetVector = Perturb(eventDriver.TimeSinceStart, targetVector);

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        cruiser.Cruise(shipControl, eventDriver, TARGET_MINING_SPEED);

        eventDriver.Schedule(FramesPerRun, Mine);
    }

    private void StartReverse(ShipControlCommons shipControl, EventDriver eventDriver)
    {
        shipControl.Reset(gyroOverride: true);

        var shipBackward = Base6Directions.GetFlippedDirection(shipControl.ShipForward);
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipBackward);
        cruiser.Init(shipControl,
                     localForward: Base6Directions.Direction.Backward);

        if (Mode != REVERSING)
        {
            Mode = REVERSING;
            eventDriver.Schedule(0, Reverse);
        }
    }

    public void Reverse(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != REVERSING) return;

        var shipControl = (ShipControlCommons)commons;

        var targetVector = GetTarget(shipControl, -StartDirection);
        targetVector = Perturb(eventDriver.TimeSinceStart, targetVector);

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        cruiser.Cruise(shipControl, eventDriver, TARGET_MINING_SPEED);

        eventDriver.Schedule(FramesPerRun, Reverse);
    }

    private void SetTarget(ShipControlCommons shipControl)
    {
        // Only set if neither mining nor reversing
        if (Mode == IDLE)
        {
            StartPoint = shipControl.ReferencePoint;
            StartDirection = shipControl.ReferenceForward;
            StartUp = shipControl.ReferenceUp;
            StartLeft = shipControl.ReferenceLeft;
        }
    }

    private Vector3D GetTarget(ShipControlCommons shipControl,
                               Vector3D direction)
    {
        // Vector from start to current
        var startVector = shipControl.ReferencePoint - StartPoint;
        // Determine projection on start direction vector
        var startDot = startVector.Dot(direction);

        Vector3D targetVector;
        if (startDot >= 0.0)
        {
            // Set targetVector to that projection
            targetVector = startDot * direction;
        }
        else
        {
            // Or set it to the start (0,0,0) if the ship is
            // behind the start position
            targetVector = new Vector3D(0, 0, 0);
        }

        // Offset forward by some amount
        targetVector += LOS_OFFSET * direction;

        // Offset by difference between start and current positions
        targetVector += StartPoint - shipControl.ReferencePoint;

        return targetVector;
    }

    private Vector3D Perturb(TimeSpan timeSinceStart, Vector3D original)
    {
        original += StartUp * MINING_PERTURB_AMPLITUDE * Math.Cos(MINING_PERTURB_SCALE * timeSinceStart.TotalSeconds);
        original += StartLeft * MINING_PERTURB_AMPLITUDE * Math.Sin(MINING_PERTURB_SCALE * timeSinceStart.TotalSeconds);
        return original;
    }

    private void SaveTarget(ZACommons commons)
    {
        string[] output = new string[13];
        output[0] = Mode.ToString();
        SaveVector3D(StartPoint, output, 1);
        SaveVector3D(StartDirection, output, 4);
        SaveVector3D(StartUp, output, 7);
        SaveVector3D(StartLeft, output, 10);
        commons.SetValue(LOSMinerKey, string.Join(";", output));
    }

    // Yes, I realize I could have just used Vector3D's ToString() and
    // TryParse(), but I'd rather not rely on them, especially ToString()
    private void SaveVector3D(Vector3D v, string[] output, int offset)
    {
        for (int i = 0; i < 3; i++)
        {
            output[offset+i] = v.GetDim(i).ToString();
        }
    }

    private void ForgetTarget(ZACommons commons)
    {
        commons.SetValue(LOSMinerKey, null);
    }
}
