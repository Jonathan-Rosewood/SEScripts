public class SolarGyroController
{
    public struct SolarPanelDetails
    {
        public float MaxPowerOutput;
        public float DefinedPowerOutput;

        public SolarPanelDetails(ZALibrary.Ship ship)
        {
            MaxPowerOutput = 0.0f;
            DefinedPowerOutput = 0.0f;

            for (var e = ship.GetBlocksOfType<IMySolarPanel>().GetEnumerator(); e.MoveNext();)
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
    private readonly string GyroGroup;

    private readonly TimeSpan AxisTimeout = TimeSpan.FromSeconds(15);

    private float? MaxPower = null;
    private int AxisIndex = 0;
    private bool Active = true;
    private TimeSpan TimeOnAxis;

    public SolarGyroController(params int[] allowedAxes) :
        this(null, allowedAxes)
    {
    }

    public SolarGyroController(string gyroGroup, params int[] allowedAxes)
    {
        // Weird things happening with array constants
        AllowedAxes = (int[])allowedAxes.Clone();
        LastVelocities = new float[AllowedAxes.Length];
        for (int i = 0; i < LastVelocities.Length; i++)
        {
            LastVelocities[i] = SOLAR_GYRO_VELOCITY;
        }
        GyroGroup = gyroGroup;
    }

    public void Run(MyGridProgram program, ZALibrary.Ship ship, string argument)
    {
        List<IMyGyro> gyros;
        if (GyroGroup != null)
        {
            var group = ZALibrary.GetBlockGroupWithName(program, GyroGroup);
            if (group == null)
            {
                throw new Exception("Group " + GyroGroup + " missing!");
            }

            gyros = ZALibrary.GetBlocksOfType<IMyGyro>(group.Blocks);
        }
        else
        {
            gyros = ship.GetBlocksOfType<IMyGyro>(test => test.IsFunctional && test.Enabled);
        }
        if (gyros.Count != 1) return; // TODO

        var gyro = gyros[0];

        // Handle commands
        argument = argument.Trim().ToLower();
        if (argument == "pause")
        {
            Active = false;
            ZAFlightLibrary.EnableGyroOverride(gyro, false);
        }
        else if (argument == "resume")
        {
            Active = true;
            ZAFlightLibrary.EnableGyroOverride(gyro, true);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        if (!Active)
        {
            program.Echo("Solar Max Power: Paused");
            return;
        }

        var currentAxis = AllowedAxes[AxisIndex];

        if (MaxPower == null)
        {
            MaxPower = -100.0f; // Start with something absurdly low to kick things off
            ZAFlightLibrary.ResetGyro(gyro);
            ZAFlightLibrary.SetAxisVelocity(gyro, currentAxis, LastVelocities[AxisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        var solarPanelDetails = new SolarPanelDetails(ship);
        var currentMaxPower = solarPanelDetails.MaxPowerOutput;

        var minError = solarPanelDetails.DefinedPowerOutput * 0.005f; // From experimentation
        var delta = currentMaxPower - MaxPower;
        MaxPower = currentMaxPower;

        if (delta > minError)
        {
            // Keep going
            ZAFlightLibrary.EnableGyroOverride(gyro, true);
        }
        else if (delta < -minError)
        {
            // Back up
            ZAFlightLibrary.EnableGyroOverride(gyro, true);
            ZAFlightLibrary.ReverseAxisVelocity(gyro, currentAxis);
        }
        else
        {
            // Hold still
            ZAFlightLibrary.EnableGyroOverride(gyro, false);
        }

        TimeOnAxis += program.ElapsedTime;

        if (TimeOnAxis > AxisTimeout)
        {
            // Time out, try next axis
            LastVelocities[AxisIndex] = ZAFlightLibrary.GetAxisVelocity(gyro, currentAxis);

            AxisIndex++;
            AxisIndex %= AllowedAxes.Length;

            ZAFlightLibrary.ResetGyro(gyro);
            ZAFlightLibrary.EnableGyroOverride(gyro, true);
            ZAFlightLibrary.SetAxisVelocity(gyro, AllowedAxes[AxisIndex], LastVelocities[AxisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        program.Echo(string.Format("Solar Max Power: {0}", ZALibrary.FormatPower(currentMaxPower)));
    }
}
