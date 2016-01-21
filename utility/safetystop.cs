public static class SafetyStop
{
    public static void ThrusterCheck(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

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

                    if (HaveWorkingThrusters2(sc, Base6Directions.Direction.Forward) &&
                        HaveWorkingThrusters2(sc, Base6Directions.Direction.Backward) &&
                        HaveWorkingThrusters2(sc, Base6Directions.Direction.Left) &&
                        HaveWorkingThrusters2(sc, Base6Directions.Direction.Right) &&
                        HaveWorkingThrusters2(sc, Base6Directions.Direction.Up) &&
                        HaveWorkingThrusters2(sc, Base6Directions.Direction.Down))
                    {
                        // All looks well
                        return;
                    }

                    // Otherwise, induce a spin on two axes and hope for the best
                    var gyroControl = sc.GyroControl;
                    gyroControl.Reset();
                    gyroControl.EnableOverride(true);
                    gyroControl.SetAxisVelocityRPM(GyroControl.Yaw, 30.0f);
                    gyroControl.SetAxisVelocityRPM(GyroControl.Pitch, 23.0f);
                });
    }

    private static bool HaveWorkingThrusters(ShipControlCommons shipControl,
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
    private static bool HaveWorkingThrusters2(ShipControlCommons shipControl,
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
