//@ commons eventdriver pid
public class RotorStepper
{
    private const uint TicksPerRun = 1;
    private const double RunsPerSecond = 60.0 / TicksPerRun;

    private const double BaseRotorStep = Math.PI / 180.0;
    private int StepFactor = -1;

    private const double RotorKp = 1.0;
    private const double RotorKi = 0.0;
    private const double RotorKd = 0.01;

    private readonly PIDController pid = new PIDController(1.0 / RunsPerSecond);

    public double SetPoint
    {
        get { return m_setPoint; }
        set
        {
            m_setPoint = value;
            if (m_setPoint < 0.0)
            {
                m_setPoint += Math.PI * 2.0;
            }
            else if (m_setPoint > Math.PI * 2.0)
            {
                m_setPoint -= Math.PI * 2.0;
            }
            pid.Reset();
        }
    }
    private double m_setPoint = 0.0;

    private readonly string RotorGroupName;
    private readonly string CommandPrefix;
    private double StepAmount = Math.PI / 900.0; // MinError = 1/2 of this
    private bool Active = false;

    public RotorStepper(string groupName, string commandPrefix = null)
    {
        RotorGroupName = groupName;
        CommandPrefix = string.IsNullOrWhiteSpace(commandPrefix) ? null : commandPrefix;

        pid.Kp = RotorKp;
        pid.Ki = RotorKi;
        pid.Kd = RotorKd;
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
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
            case "left":
                SetPoint -= StepAmount;
                Schedule(eventDriver);
                break;
            case "right":
                SetPoint += StepAmount;
                Schedule(eventDriver);
                break;
            case "reset":
                SetPoint = 0.0;
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
                StepFactor = -1;
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

        var rotor = GetRotor(commons);

        //commons.Echo("Angle: " + rotor.Angle);
        //commons.Echo("SetPoint: " + SetPoint);
        var error = SetPoint - rotor.Angle;
        if (error > Math.PI) error -= Math.PI * 2.0;
        if (error < -Math.PI) error += Math.PI * 2.0;
        //commons.Echo("Error: " + error);

        if (Math.Abs(error) > StepAmount / 2.0)
        {
            var rotorVelocity = pid.Compute(error);
            rotor.SetValue<float>("Velocity", (float)(rotor.GetMaximum<float>("Velocity") * rotorVelocity));
            eventDriver.Schedule(TicksPerRun, Run);
        }
        else
        {
            rotor.SetValue<float>("Velocity", 0.0f);
            Active = false;
            // NB We don't re-schedule
        }
    }

    public void Schedule(EventDriver eventDriver)
    {
        if (!Active)
        {
            Active = true;
            eventDriver.Schedule(0, Run);
        }
    }

    private void UpdateStepAmount(ZACommons commons)
    {
        StepAmount = BaseRotorStep * Math.Pow(10.0, StepFactor);
        var rotor = GetRotor(commons);
        SetPoint = rotor.Angle;
    }

    public IMyMotorStator GetRotor(ZACommons commons)
    {
        var rotorGroup = commons.GetBlockGroupWithName(RotorGroupName);
        if (rotorGroup == null)
        {
            throw new Exception("Missing group: " + RotorGroupName);
        }
        var rotors = ZACommons.GetBlocksOfType<IMyMotorStator>(rotorGroup.Blocks);
        if (rotors.Count != 1)
        {
            throw new Exception("Expecting exactly 1 rotor in " + RotorGroupName);
        }

        return rotors[0];
    }
}
