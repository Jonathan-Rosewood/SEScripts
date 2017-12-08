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
            if (parts.Length == 7)
            {
                UndockTarget = new Vector3D(double.Parse(parts[0]),
                                            double.Parse(parts[1]),
                                            double.Parse(parts[2]));
                var orientation = new QuaternionD(double.Parse(parts[3]),
                                                  double.Parse(parts[4]),
                                                  double.Parse(parts[5]),
                                                  double.Parse(parts[6]));
                UndockForward = Transform(Vector3D.Forward, orientation);
                UndockUp = Transform(Vector3D.Up, orientation);
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
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    // Assume the first one as well
                    connected = connector;
                    break;
                }
            }

            UndockTarget = null;
            UndockBackward = null;
            Vector3D forwardVector = Vector3D.Zero;
            if (connected != null)
            {
                // Undock in the forward direction of the *other* connector
                var other = connected.OtherConnector;
                forwardVector = other.WorldMatrix.Forward;
                UndockBackward = connected.Orientation.TransformDirection(Base6Directions.Direction.Backward);
            }

            // Next, physically undock
            foreach (var block in connectors)
            {
                var connector = (IMyShipConnector)block;
                if (connector.Status == MyShipConnectorStatus.Connected) connector.ApplyAction("Unlock");
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

            if (connected != null)
            {
                eventDriver.Schedule(1.0, (c, ed) =>
                        {
                            // Determine target undock point
                            var shipControl = (ShipControlCommons)c;
                            UndockTarget = shipControl.ReferencePoint + SMART_UNDOCK_DISTANCE * forwardVector;

                            // And original orientation
                            UndockForward = shipControl.ReferenceForward;
                            UndockUp = shipControl.ReferenceUp;

                            // Schedule the autopilot
                            BeginUndock(c, ed);
                            Mode = UNDOCKING;
                            SaveMode(c);
                            SaveUndockTarget(c);
                        });
            }
            else SaveUndockTarget(commons);
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
        double yawPitchError, rollError;
        seeker.Seek(shipControl, UndockForward, UndockUp,
                    out yawPitchError, out rollError);

        if (yawPitchError < .005 && rollError < .01)
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
            var orientation = QuaternionD.CreateFromForwardUp(UndockForward, UndockUp);
            value = string.Format("{0};{1};{2};{3};{4};{5};{6}",
                                  ((Vector3D)UndockTarget).X,
                                  ((Vector3D)UndockTarget).Y,
                                  ((Vector3D)UndockTarget).Z,
                                  orientation.X,
                                  orientation.Y,
                                  orientation.Z,
                                  orientation.W);
        }
        commons.SetValue(UndockTargetKey, value);

        value = null;
        if (UndockBackward != null)
        {
            value = ((byte)UndockBackward).ToString();
        }
        commons.SetValue(BackwardKey, value);
    }

    // Why no QuaternionD version?
    private static Vector3D Transform(Vector3D value, QuaternionD rotation)
    {
        double num1 = rotation.X + rotation.X;
        double num2 = rotation.Y + rotation.Y;
        double num3 = rotation.Z + rotation.Z;
        double num4 = rotation.W * num1;
        double num5 = rotation.W * num2;
        double num6 = rotation.W * num3;
        double num7 = rotation.X * num1;
        double num8 = rotation.X * num2;
        double num9 = rotation.X * num3;
        double num10 = rotation.Y * num2;
        double num11 = rotation.Y * num3;
        double num12 = rotation.Z * num3;
        double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
        double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
        double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
        Vector3D vector3;
        vector3.X = num13;
        vector3.Y = num14;
        vector3.Z = num15;
        return vector3;
    }
}
