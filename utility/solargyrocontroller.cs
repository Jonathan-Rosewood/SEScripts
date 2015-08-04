public class SolarGyroController
{
    public struct SolarPanelDetails
    {
        public float MaxPowerOutput;
        public float DefinedPowerOutput;

        public SolarPanelDetails(IEnumerable<IMyTerminalBlock> blocks)
        {
            MaxPowerOutput = 0.0f;
            DefinedPowerOutput = 0.0f;

            for (var e = ZACommons.GetBlocksOfType<IMySolarPanel>(blocks).GetEnumerator(); e.MoveNext();)
            {
                var panel = e.Current;

                if (panel.IsFunctional && panel.IsWorking)
                {
                    MaxPowerOutput += panel.MaxPowerOutput;
                    DefinedPowerOutput += panel.DefinedPowerOutput;
                }
            }
        }
    }

    private readonly int[] AllowedAxes;
    private readonly float[] LastVelocities;

    private readonly TimeSpan AxisTimeout = TimeSpan.FromSeconds(15);

    private float? MaxPower = null;
    private int AxisIndex = 0;
    private bool Active = true;
    private TimeSpan TimeOnAxis;

    public SolarGyroController(params int[] allowedAxes)
    {
        // Weird things happening with array constants
        AllowedAxes = (int[])allowedAxes.Clone();
        LastVelocities = new float[AllowedAxes.Length];
        for (int i = 0; i < LastVelocities.Length; i++)
        {
            LastVelocities[i] = SOLAR_GYRO_VELOCITY;
        }
    }

    private GyroControl GetGyroControl(ZACommons commons,
                                       Base6Directions.Direction shipUp,
                                       Base6Directions.Direction shipForward)
    {
        var gyroControl = new GyroControl();
        gyroControl.Init(commons.Blocks, shipUp: shipUp, shipForward: shipForward);
        return gyroControl;
    }

    public void Run(ZACommons commons,
                    Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                    Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        if (!Active)
        {
            commons.Echo("Solar Max Power: Paused");
            return;
        }

        var gyroControl = GetGyroControl(commons, shipUp, shipForward);
        var currentAxis = AllowedAxes[AxisIndex];

        if (MaxPower == null)
        {
            MaxPower = -100.0f; // Start with something absurdly low to kick things off
            gyroControl.Reset();
            gyroControl.EnableOverride(true);
            gyroControl.SetAxisVelocity(currentAxis, LastVelocities[AxisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        var solarPanelDetails = new SolarPanelDetails(commons.Blocks);
        var currentMaxPower = solarPanelDetails.MaxPowerOutput;

        var minError = solarPanelDetails.DefinedPowerOutput * 0.005f; // From experimentation
        var delta = currentMaxPower - MaxPower;
        MaxPower = currentMaxPower;

        if (delta > minError)
        {
            // Keep going
            gyroControl.EnableOverride(true);
        }
        else if (delta < -minError)
        {
            // Back up
            gyroControl.EnableOverride(true);
            LastVelocities[AxisIndex] = -LastVelocities[AxisIndex];
            gyroControl.SetAxisVelocity(currentAxis, LastVelocities[AxisIndex]);
        }
        else
        {
            // Hold still
            gyroControl.EnableOverride(false);
        }

        TimeOnAxis += commons.Program.ElapsedTime;

        if (TimeOnAxis > AxisTimeout)
        {
            // Time out, try next axis
            AxisIndex++;
            AxisIndex %= AllowedAxes.Length;

            gyroControl.Reset();
            gyroControl.EnableOverride(true);
            gyroControl.SetAxisVelocity(AllowedAxes[AxisIndex], LastVelocities[AxisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        commons.Echo(string.Format("Solar Max Power: {0}", ZACommons.FormatPower(currentMaxPower)));
    }

    public void HandleCommand(ZACommons commons, string argument,
                              Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                              Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        // Handle commands
        argument = argument.Trim().ToLower();
        if (argument == "pause")
        {
            // Hmm, shipUp and shipForward not really needed...
            var gyroControl = GetGyroControl(commons, shipUp, shipForward);

            Active = false;
            gyroControl.EnableOverride(false);
        }
        else if (argument == "resume")
        {
            Active = true;
            MaxPower = null; // Use first-run initialization
        }
    }
}
