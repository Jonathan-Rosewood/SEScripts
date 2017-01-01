//@ commons eventdriver pid
public class PistonStepper
{
    private const uint TicksPerRun = 1;
    private const double RunsPerSecond = 60.0 / TicksPerRun;

    private double BasePistonStep;
    private int StepFactor;

    private const double PistonKp = 5.0;
    private const double PistonKi = 0.0;
    private const double PistonKd = 0.0;

    private readonly PIDController pid = new PIDController(1.0 / RunsPerSecond);

    private double SetPoint
    {
        get { return m_setPoint; }
        set
        {
            m_setPoint = value;
            pid.Reset();
        }
    }
    private double m_setPoint = 0.0;

    private readonly string PistonGroupName;
    private readonly string CommandPrefix;
    private double StepAmount;
    private bool Active = false;

    public PistonStepper(string groupName, string commandPrefix = null,
                         int startingStepFactor = 0)
    {
        PistonGroupName = groupName;
        CommandPrefix = string.IsNullOrWhiteSpace(commandPrefix) ? null : commandPrefix;
        StepFactor = startingStepFactor;
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        pid.Kp = PistonKp;
        pid.Ki = PistonKi;
        pid.Kd = PistonKd;

        var piston = GetPiston(commons);
        BasePistonStep = piston.CubeGrid.GridSize;

        UpdateStepAmount(commons);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();

        if (CommandPrefix != null)
        {
            var parts = argument.Split(new char[] { ' ' }, 2);
            if (parts.Length != 2 ||
                !parts[0].Equals(CommandPrefix, ZACommons.IGNORE_CASE)) return;
            argument = parts[1];
        }

        switch (argument)
        {
            case "retract":
                SetPoint -= StepAmount;
                ConstrainSetPoint(commons);
                Schedule(eventDriver);
                break;
            case "extend":
                SetPoint += StepAmount;
                ConstrainSetPoint(commons);
                Schedule(eventDriver);
                break;
            case "min":
                SetPoint = 0.0;
                ConstrainSetPoint(commons);
                Schedule(eventDriver);
                break;
            case "max":
                SetPoint = 9000.0;
                ConstrainSetPoint(commons);
                Schedule(eventDriver);
                break;
            case "factorup":
                StepFactor++;
                UpdateStepAmount(commons);
                Schedule(eventDriver);
                break;
            case "factordown":
                StepFactor--;
                UpdateStepAmount(commons);
                Schedule(eventDriver);
                break;
            case "factorreset":
                StepFactor = 0;
                UpdateStepAmount(commons);
                Schedule(eventDriver);
                break;
            default:
                break;
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!Active) return;

        var piston = GetPiston(commons);

        var error = SetPoint - piston.CurrentPosition;

        if (Math.Abs(error) > PISTONSTEPPER_MIN_ERROR)
        {
            var pistonVelocity = pid.Compute(error);
            piston.SetValue<float>("Velocity", (float)(piston.GetMaximum<float>("Velocity") * pistonVelocity));
            eventDriver.Schedule(TicksPerRun, Run);
        }
        else
        {
            piston.SetValue<float>("Velocity", 0.0f);
            Active = false;
            // NB We don't re-schedule
        }
    }

    private void Schedule(EventDriver eventDriver)
    {
        if (!Active)
        {
            Active = true;
            eventDriver.Schedule(0, Run);
        }
    }

    private void ConstrainSetPoint(ZACommons commons)
    {
        var piston = GetPiston(commons);
        if (SetPoint < piston.MinLimit)
        {
            SetPoint = piston.MinLimit;
        }
        else if (SetPoint > piston.MaxLimit)
        {
            SetPoint = piston.MaxLimit;
        }
    }

    private void UpdateStepAmount(ZACommons commons)
    {
        StepAmount = BasePistonStep * Math.Pow(10.0, StepFactor);
        var piston = GetPiston(commons);
        SetPoint = piston.CurrentPosition;
    }

    private IMyPistonBase GetPiston(ZACommons commons)
    {
        var pistonGroup = commons.GetBlockGroupWithName(PistonGroupName);
        if (pistonGroup == null)
        {
            throw new Exception("Missing group: " + PistonGroupName);
        }
        var pistons = ZACommons.GetBlocksOfType<IMyPistonBase>(pistonGroup.Blocks);
        if (pistons.Count != 1)
        {
            throw new Exception("Expecting exactly 1 piston in " + PistonGroupName);
        }

        return pistons[0];
    }
}
