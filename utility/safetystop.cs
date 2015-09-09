public static class SafetyStop
{
    public static void ThrusterCheck(ZACommons commons)
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

        // Otherwise, induce a spin on two axes and hope for the best
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(true);
        gyroControl.SetAxisVelocityRPM(GyroControl.Yaw, 30.0f);
        gyroControl.SetAxisVelocityRPM(GyroControl.Pitch, 23.0f);
    }

    private static bool HaveWorkingThrusters(ShipControlCommons shipControl,
                                             Base6Directions.Direction direction)
    {
        // Look for a working, non-overridden thruster
        var found = false;
        var thrusters = shipControl.ThrustControl.GetThrusters(direction);
        for (var e = thrusters.GetEnumerator(); e.MoveNext();)
        {
            var thruster = e.Current;
            if (thruster.IsFunctional && thruster.Enabled &&
                thruster.IsWorking)
            {
                if (thruster.GetValue<float>("Override") > 0.0f)
                {
                    // Thruster is overidden. Turn it off.
                    thruster.SetValue<bool>("OnOff", false);
                }
                else
                {
                    // Found a good thruster
                    found = true;
                }
            }
        }
        return found;
    }
}
