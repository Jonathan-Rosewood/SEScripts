//@ shipcontrol eventdriver yawpitchauto seeker
public class SmartUndock
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;
    private const string UndockTargetKey = "SmartUndock_UndockTarget";
    private const string ModeKey = "SmartUndock_Mode";
    private const string BackwardKey = "SmartUndock_Backward";

    private readonly YawPitchAutopilot autopilot = new YawPitchAutopilot();
    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Vector3D? UndockTarget = null;
    private Vector3D UndockForward, UndockUp;
    private Base6Directions.Direction? UndockBackward = null;

    private const int IDLE = 0;
    private const int UNDOCKING = 1;
    private const int RETURNING = 2;
    private const int ORIENTING = 3;

    private int Mode = IDLE;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        UndockTarget = null;
        var previousTarget = commons.GetValue(UndockTargetKey);
        if (previousTarget != null)
        {
            var parts = previousTarget.Split(';');
            if (parts.Length == 9)
            {
                var newTarget = new Vector3D();
                UndockForward = new Vector3D();
                UndockUp = new Vector3D();
                for (int i = 0; i < 3; i++)
                {
                    newTarget.SetDim(i, double.Parse(parts[i]));
                    UndockForward.SetDim(i, double.Parse(parts[3+i]));
                    UndockUp.SetDim(i, double.Parse(parts[6+i]));
                }
                UndockTarget = newTarget; // Set only if all successfully parsed
            }
        }

        UndockBackward = null;
        var backwardString = commons.GetValue(BackwardKey);
        if (backwardString != null)
        {
            // Enum.Parse apparently works, but I don't trust Keen
            // not breaking it in the future (by making typeof() illegal)
            //UndockBackward = (Base6Directions.Direction)Enum.Parse(typeof(Base6Directions.Direction), backwardString);
            UndockBackward = (Base6Directions.Direction)byte.Parse(backwardString);
        }

        Mode = IDLE;
        var modeString = commons.GetValue(ModeKey);
        if (modeString != null)
        {
            Mode = int.Parse(modeString);

            switch (Mode)
            {
                case IDLE:
                    break;
                case UNDOCKING:
                    if (UndockTarget != null && UndockBackward != null)
                    {
                        BeginUndock(commons, eventDriver);
                    }
                    else ResetMode(commons);
                    break;
                case RETURNING:
                    if (UndockTarget != null)
                    {
                        BeginReturn(commons, eventDriver);
                    }
                    else ResetMode(commons);
                    break;
                case ORIENTING:
                    if (UndockTarget != null)
                    {
                        ReorientStart(commons, eventDriver);
                    }
                    else ResetMode(commons);
                    break;
            }
        }
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument, Action preUndock = null)
    {
        argument = argument.Trim().ToLower();
        if (argument == "smartundock")
        {
            if (preUndock != null) preUndock();

            // First, determine which connector we were connected through
            IMyShipConnector connected = null;
            var connectors = ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks,
                                                                         connector => connector.DefinitionDisplayNameText == "Connector"); // Avoid Ejectors
            foreach (var connector in connectors)
            {
                if (connector.IsLocked && connector.IsConnected)
                {
                    // Assume the first one as well
                    connected = connector;
                    break;
                }
            }

            UndockTarget = null;
            UndockBackward = null;
            if (connected != null)
            {
                // Undock in the forward direction of the *other* connector
                var other = connected.OtherConnector;
                var forward = other.Orientation.TransformDirection(Base6Directions.Direction.Forward);
                var forwardPoint = other.CubeGrid.GridIntegerToWorld(other.Position + Base6Directions.GetIntVector(forward));
                var forwardVector = Vector3D.Normalize(forwardPoint - other.GetPosition());
                // Determine target undock point
                var shipControl = (ShipControlCommons)commons;
                UndockTarget = shipControl.ReferencePoint + SMART_UNDOCK_DISTANCE * forwardVector;

                // And original orientation
                UndockForward = shipControl.ReferenceForward;
                UndockUp = shipControl.ReferenceUp;

                // Schedule the autopilot
                UndockBackward = connected.Orientation.TransformDirection(Base6Directions.Direction.Backward);
                BeginUndock(commons, eventDriver);
                Mode = UNDOCKING;
                SaveMode(commons);
            }
            SaveUndockTarget(commons);

            // Next, physically undock
            foreach (var block in connectors)
            {
                var connector = (IMyShipConnector)block;
                if (connector.IsLocked) connector.ApplyAction("Unlock");
            }
            ZACommons.EnableBlocks(connectors, false);
            // Unlock landing gears as well
            var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
            gears.ForEach(gear =>
                    {
                        if (gear.IsLocked) gear.ApplyAction("Unlock");
                    });

            // Disable connectors 1 second from now
            eventDriver.Schedule(1.0, (c, ed) =>
                    {
                        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyShipConnector>(c.Blocks, connector => connector.DefinitionDisplayNameText == "Connector"),
                                               false); // Avoid Ejectors
                    });
        }
        else if (argument == "rtb")
        {
            // No target, no RTB
            if (UndockTarget == null) return;

            // Schedule the autopilot
            BeginReturn(commons, eventDriver);
            Mode = RETURNING;
            SaveMode(commons);
        }
        else if (argument == "smartreset")
        {
            autopilot.Reset(commons);
            ResetMode(commons);
        }
    }

    public void ReorientStart(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        Mode = ORIENTING;
        SaveMode(commons);

        eventDriver.Schedule(0, Reorient);
    }

    public void Reorient(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != ORIENTING) return;

        var shipControl = (ShipControlCommons)commons;
        double yawError, pitchError, rollError;
        seeker.Seek(shipControl, UndockForward, UndockUp,
                    out yawError, out pitchError, out rollError);

        if ((yawError * yawError + pitchError * pitchError + rollError * rollError) < 0.0001)
        {
            // All done
            shipControl.Reset(gyroOverride: false, thrusterEnable: null);
            ResetMode(commons);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Reorient);
        }
    }

    public void Display(ZACommons commons)
    {
        switch (Mode)
        {
            case IDLE:
                break;
            case UNDOCKING:
                commons.Echo("SmartUndock: Undocking");
                break;
            case RETURNING:
                commons.Echo("SmartUndock: Returning");
                break;
            case ORIENTING:
                commons.Echo("SmartUndock: Orienting");
                break;
        }
    }

    private void BeginUndock(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        autopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                       SMART_UNDOCK_UNDOCK_SPEED,
                       delay: 2.0,
                       localForward: shipControl.ShipBlockOrientation.TransformDirectionInverse((Base6Directions.Direction)UndockBackward),
                       doneAction: (c,ed) => {
                           ResetMode(c);
                       });
    }

    private void BeginReturn(ZACommons commons, EventDriver eventDriver)
    {
        autopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                       SMART_UNDOCK_RTB_SPEED,
                       doneAction: ReorientStart);
    }

    private void ResetMode(ZACommons commons)
    {
        Mode = IDLE;
        SaveMode(commons);
    }

    private void SaveMode(ZACommons commons)
    {
        commons.SetValue(ModeKey, Mode.ToString());
    }

    private void SaveUndockTarget(ZACommons commons)
    {
        string value = null;
        if (UndockTarget != null)
        {
            value = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                                  ((Vector3D)UndockTarget).GetDim(0),
                                  ((Vector3D)UndockTarget).GetDim(1),
                                  ((Vector3D)UndockTarget).GetDim(2),
                                  UndockForward.GetDim(0),
                                  UndockForward.GetDim(1),
                                  UndockForward.GetDim(2),
                                  UndockUp.GetDim(0),
                                  UndockUp.GetDim(1),
                                  UndockUp.GetDim(2));
        }
        commons.SetValue(UndockTargetKey, value);

        value = null;
        if (UndockBackward != null)
        {
            value = ((byte)UndockBackward).ToString();
        }
        commons.SetValue(BackwardKey, value);
    }
}
