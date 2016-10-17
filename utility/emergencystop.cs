//@ shipcontrol eventdriver safemodehandler reversethrust
public class EmergencyStop : SafeModeHandler
{
    public void SafeMode(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Ensure dampeners are enabled (again, possibly)
        ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks).ForEach(block => {
                var controller = (IMyShipController)block;
                block.SetValue<bool>("DampenersOverride", true);
            });

        // Make sure we have working thrusters in all directions

        // Statically initialized arrays are a bit iffy in-game, so we
        // do this the hard way
        if (HaveWorkingThrusters(shipControl, Base6Directions.Direction.Forward) &&
            HaveWorkingThrusters(shipControl, Base6Directions.Direction.Backward) &&
            HaveWorkingThrusters(shipControl, Base6Directions.Direction.Left) &&
            HaveWorkingThrusters(shipControl, Base6Directions.Direction.Right) &&
            HaveWorkingThrusters(shipControl, Base6Directions.Direction.Up) &&
            HaveWorkingThrusters(shipControl, Base6Directions.Direction.Down))
        {
            // All looks well
            return;
        }

        // Check again after a second
        eventDriver.Schedule(1.0, (c,ed) =>
                {
                    var sc = (ShipControlCommons)c;

                    var forward = HaveWorkingThrusters2(sc, Base6Directions.Direction.Forward);
                    var backward = HaveWorkingThrusters2(sc, Base6Directions.Direction.Backward);
                    var up = HaveWorkingThrusters2(sc, Base6Directions.Direction.Up);
                    var left = HaveWorkingThrusters2(sc, Base6Directions.Direction.Left);
                    var right = HaveWorkingThrusters2(sc, Base6Directions.Direction.Right);
                    var down = HaveWorkingThrusters2(sc, Base6Directions.Direction.Down);

                    if (forward && backward && up &&
                        left && right && down)
                    {
                        // All is well
                        return;
                    }

                    // Otherwise, pick a working direction and pass off to
                    // ReverseThrust
                    Base6Directions.Direction direction;
                    if (forward)
                    {
                        direction = Base6Directions.Direction.Forward;
                    }
                    else if (backward)
                    {
                        direction = Base6Directions.Direction.Backward;
                    }
                    else if (up)
                    {
                        direction = Base6Directions.Direction.Up;
                    }
                    else if (left)
                    {
                        direction = Base6Directions.Direction.Left;
                    }
                    else if (right)
                    {
                        direction = Base6Directions.Direction.Right;
                    }
                    else if (down)
                    {
                        direction = Base6Directions.Direction.Down;
                    }
                    else
                    {
                        // Ha ha. No working thrusters at all.
                        return;
                    }

                    new ReverseThrust().Init(c, ed,
                                             EMERGENCY_STOP_MAX_GYRO_ERROR,
                                             thrusterDirection: direction);
                });
    }

    private bool HaveWorkingThrusters(ShipControlCommons shipControl,
                                             Base6Directions.Direction direction)
    {
        // First pass: Look for a working, non-overridden thruster
        var found = false;
        var overridden = new List<IMyThrust>();
        var thrusters = shipControl.ThrustControl.GetThrusters(direction);
        thrusters.ForEach(thruster =>
                {
                    if (thruster.IsWorking)
                    {
                        if (thruster.GetValue<float>("Override") > 0.0f)
                        {
                            // Thruster is overridden. Keep track of it.
                            overridden.Add(thruster);
                        }
                        else
                        {
                            // Found a good thruster
                            found = true;
                        }
                    }
                });

        // Depending on outcome, disable or zero-out overridden thrusters
        overridden.ForEach(thruster =>
                {
                    if (found)
                    {
                        // Disable and let good thrusters take care of it
                        thruster.SetValue<bool>("OnOff", false);
                    }
                    else
                    {
                        // No good thrusters. Zero-out override.
                        thruster.SetValue<float>("Override", 0.0f);
                        found = true;
                        // Note this means we will zero-out at most 1 thruster.
                        // For now, this is the desired effect.
                    }
                });

        if (!found)
        {
            // Final desperation move. Enable and zero-out overrides for
            // all thrusters on this side.
            thrusters.ForEach(thruster =>
                    {
                        thruster.SetValue<bool>("OnOff", true);
                        thruster.SetValue<float>("Override", 0.0f);
                    });
            // Still return false, but we'll check again after a few ticks
        }

        return found;
    }

    // Last-ditch check before inducing spin
    private bool HaveWorkingThrusters2(ShipControlCommons shipControl,
                                              Base6Directions.Direction direction)
    {
        var found = false;
        var thrusters = shipControl.ThrustControl.GetThrusters(direction);
        thrusters.ForEach(thruster =>
                {
                    if (thruster.IsWorking)
                    {
                        // Really make sure it isn't overridden
                        thruster.SetValue<float>("Override", 0.0f);
                        found = true;
                    }
                });

        return found;
    }
}
