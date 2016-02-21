public class SmartUndock
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;
    private const string UndockTargetKey = "SmartUndock_UndockTarget";

    private readonly TranslateAutopilot undockAutopilot = new TranslateAutopilot();
    private readonly YawPitchAutopilot rtbAutopilot = new YawPitchAutopilot();
    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Vector3D? UndockTarget = null;
    private Vector3D UndockForward, UndockUp;
    private bool Reorienting = false;

    public void Init(ZACommons commons)
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
            for (var e = connectors.GetEnumerator(); e.MoveNext();)
            {
                var connector = (IMyShipConnector)e.Current;
                if (connector.IsLocked && connector.IsConnected)
                {
                    // Assume the first one as well
                    connected = connector;
                    break;
                }
            }

            UndockTarget = null;
            if (connected != null)
            {
                // Undock the opposite direction of connector
                var forward = connected.Orientation.TransformDirection(Base6Directions.Direction.Backward);
                var up = connected.Orientation.TransformDirection(Base6Directions.Direction.Up);

                var reference = commons.Me;
                var backwardPoint = reference.CubeGrid.GridIntegerToWorld(reference.Position + Base6Directions.GetIntVector(forward));
                var backwardVector = Vector3D.Normalize(backwardPoint - reference.GetPosition());
                // Determine target undock point
                UndockTarget = reference.GetPosition() + SMART_UNDOCK_DISTANCE * backwardVector;

                // And original orientation
                var shipControl = (ShipControlCommons)commons;
                UndockForward = shipControl.ReferenceForward;
                UndockUp = shipControl.ReferenceUp;

                // Schedule the autopilot
                undockAutopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                                     SMART_UNDOCK_UNDOCK_SPEED,
                                     delay: 2.0);
                Reorienting = false;
            }
            SaveUndockTarget(commons);

            // Next, physically undock
            for (var e = connectors.GetEnumerator(); e.MoveNext();)
            {
                var connector = (IMyShipConnector)e.Current;
                if (connector.IsLocked) connector.GetActionWithName("Unlock").Apply(connector);
            }
            ZACommons.EnableBlocks(connectors, false);
            // Unlock landing gears as well
            var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
            gears.ForEach(block =>
                    {
                        var gear = (IMyLandingGear)block;
                        if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
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

            var shipControl = (ShipControlCommons)commons;

            // Schedule the autopilot
            rtbAutopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                              SMART_UNDOCK_RTB_SPEED, doneAction: (c,ed) =>
                                      {
                                          ReorientStart(c, ed);
                                      });
            Reorienting = false;
        }
        else if (argument == "smartreset")
        {
            undockAutopilot.Reset(commons);
            rtbAutopilot.Reset(commons);
            Reorienting = false;
        }
    }

    public void ReorientStart(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        Reorienting = true;
        eventDriver.Schedule(0, Reorient);
    }

    public void Reorient(ZACommons commons, EventDriver eventDriver)
    {
        if (!Reorienting) return;

        var shipControl = (ShipControlCommons)commons;
        double yawError, pitchError, rollError;
        seeker.Seek(shipControl, UndockForward, UndockUp,
                    out yawError, out pitchError, out rollError);

        if ((yawError * yawError + pitchError * pitchError + rollError * rollError) < 0.000001)
        {
            // All done
            shipControl.Reset(gyroOverride: false, thrusterEnable: null);
            Reorienting = false;
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Reorient);
        }
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
    }
}
