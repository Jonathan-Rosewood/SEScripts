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

    // Why no enums, Keen?!
    public const int GyroAxisYaw = 0;
    public const int GyroAxisPitch = 1;
    public const int GyroAxisRoll = 2;

    private readonly int[] allowedAxes;
    private readonly float[] lastVelocities;

    private readonly TimeSpan AxisTimeout = TimeSpan.FromSeconds(15);

    private bool FirstRun = true;
    private int axisIndex = 0;
    private float maxPower = -100.0f;
    private bool Active = true;
    private TimeSpan TimeOnAxis;

    public SolarGyroController(params int[] allowedAxes)
    {
        // Weird things happening with array constants
        this.allowedAxes = (int[])allowedAxes.Clone();
        lastVelocities = new float[this.allowedAxes.Length];
        for (int i = 0; i < lastVelocities.Length; i++)
        {
            lastVelocities[i] = SOLAR_GYRO_VELOCITY;
        }
    }
    
    private void EnableGyroOverride(IMyGyro gyro, bool enable)
    {
        if ((gyro.GyroOverride && !enable) ||
            (!gyro.GyroOverride && enable))
        {
            gyro.GetActionWithName("Override").Apply(gyro);
        }
    }

    private void SetAxisVelocity(IMyGyro gyro, int axis, float velocity)
    {
        switch (axis)
        {
            case GyroAxisYaw:
                gyro.SetValue<float>("Yaw", velocity);
                break;
            case GyroAxisPitch:
                gyro.SetValue<float>("Pitch", velocity);
                break;
            case GyroAxisRoll:
                gyro.SetValue<float>("Roll", velocity);
                break;
        }
    }

    private float GetAxisVelocity(IMyGyro gyro, int axis)
    {
        switch (axis)
        {
            case GyroAxisYaw:
                return gyro.Yaw;
            case GyroAxisPitch:
                return gyro.Pitch;
            case GyroAxisRoll:
                return gyro.Roll;
        }
        return default(float);
    }

    private void ReverseAxisVelocity(IMyGyro gyro, int axis)
    {
        float? velocity = null;

        switch (axis)
        {
            case GyroAxisYaw:
                velocity = -gyro.Yaw;
                break;
            case GyroAxisPitch:
                velocity = -gyro.Pitch;
                break;
            case GyroAxisRoll:
                velocity = -gyro.Roll;
                break;
        }

        if (velocity != null) SetAxisVelocity(gyro, axis, (float)velocity);
    }

    private void ResetGyro(IMyGyro gyro)
    {
        SetAxisVelocity(gyro, GyroAxisYaw, 0.0f);
        SetAxisVelocity(gyro, GyroAxisPitch, 0.0f);
        SetAxisVelocity(gyro, GyroAxisRoll, 0.0f);
    }

    public void Run(MyGridProgram program, ZALibrary.Ship ship, string argument)
    {
        var gyros = ship.GetBlocksOfType<IMyGyro>();
        if (gyros.Count != 1) return; // TODO

        var gyro = gyros[0];

        // Handle commands
        argument = argument.Trim().ToLower();
        if (argument == "pause")
        {
            Active = false;
            EnableGyroOverride(gyro, false);
        }
        else if (argument == "resume")
        {
            Active = true;
            EnableGyroOverride(gyro, true);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        if (!Active)
        {
            program.Echo("Solar Max Power: Paused");
            return;
        }

        var currentAxis = allowedAxes[axisIndex];

        // FIXME
        if (FirstRun)
        {
            FirstRun = false;
            ResetGyro(gyro);
            SetAxisVelocity(gyro, currentAxis, lastVelocities[axisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        var solarPanelDetails = new SolarPanelDetails(ship);
        var currentMaxPower = solarPanelDetails.MaxPowerOutput;

        var minError = solarPanelDetails.DefinedPowerOutput * 0.005f; // From experimentation
        var delta = currentMaxPower - maxPower;
        maxPower = currentMaxPower;

        if (delta > minError)
        {
            // Keep going
            EnableGyroOverride(gyro, true);
        }
        else if (delta < -minError)
        {
            // Back up
            EnableGyroOverride(gyro, true);
            ReverseAxisVelocity(gyro, currentAxis);
        }
        else
        {
            // Hold still
            EnableGyroOverride(gyro, false);
        }

        TimeOnAxis += program.ElapsedTime;

        if (TimeOnAxis > AxisTimeout)
        {
            // Time out, try next axis
            lastVelocities[axisIndex] = GetAxisVelocity(gyro, currentAxis);

            axisIndex++;
            axisIndex %= allowedAxes.Length;

            ResetGyro(gyro);
            EnableGyroOverride(gyro, true);
            SetAxisVelocity(gyro, allowedAxes[axisIndex], lastVelocities[axisIndex]);
            TimeOnAxis = TimeSpan.FromSeconds(0);
        }

        program.Echo(String.Format("Solar Max Power: {0}", ZALibrary.FormatPower(currentMaxPower)));
    }
}
