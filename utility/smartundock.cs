public class SmartUndock
{
    private readonly YawPitchAutopilot yawPitchAutopilot = new YawPitchAutopilot();

    private Vector3D? UndockTarget = null;

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        if (argument == "smartundock")
        {
            // First, determine which connector we were connected through
            IMyShipConnector connected = null;
            var connectors = ZACommons.GetBlocksOfType<IMyShipConnector>(commons.Blocks,
                                                                         connector => connector.DefinitionDisplayNameText == "Connector"); // Avoid Ejectors
            for (var e = connectors.GetEnumerator(); e.MoveNext();)
            {
                var connector = e.Current;
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

                // Schedule the autopilot
                yawPitchAutopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                                       SMART_UNDOCK_UNDOCK_SPEED,
                                       autopilotUp: up,
                                       autopilotForward: forward,
                                       delay: 2.0);
            }

            // Next, physically undock
            ZACommons.EnableBlocks(connectors, false);
            // Unlock landing gears as well
            var gears = ZACommons.GetBlocksOfType<IMyLandingGear>(commons.Blocks);
            gears.ForEach(gear =>
                    {
                        if (gear.IsLocked) gear.GetActionWithName("Unlock").Apply(gear);
                    });
        }
        else if (argument == "rtb")
        {
            // No target, no RTB
            if (UndockTarget == null) return;

            var shipControl = (ShipControlCommons)commons;

            // Schedule the autopilot
            yawPitchAutopilot.Init(commons, eventDriver, (Vector3D)UndockTarget,
                                   SMART_UNDOCK_RTB_SPEED,
                                   autopilotUp: shipControl.ShipUp,
                                   autopilotForward: shipControl.ShipForward);
        }
        else if (argument == "smartreset")
        {
            yawPitchAutopilot.Reset(commons);
        }
    }
}
