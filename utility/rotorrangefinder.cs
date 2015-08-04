public class RotorRangefinder
{
    private const int TicksPerRun = 5;
    private const double RunsPerSecond = 60.0 / TicksPerRun;

    private const double BaseRotorStep = Math.PI / 180.0;
    private int StepFactor = -1;

    private const double RotorKp = 100.0;
    private const double RotorKi = 0.0;
    private const double RotorKd = 0.0;

    private readonly PIDController pid = new PIDController(1.0 / RunsPerSecond);

    private double SetPoint
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

    private double MinError; // Also our current step size

    private IMyMotorStator GetRotor(ZACommons commons)
    {
        var rotorGroup = commons.GetBlockGroupWithName(ROTOR_REFERENCE_GROUP);
        if (rotorGroup == null)
        {
            throw new Exception("Missing group: " + ROTOR_REFERENCE_GROUP);
        }
        var rotors = ZACommons.GetBlocksOfType<IMyMotorStator>(rotorGroup.Blocks);
        if (rotors.Count != 1)
        {
            throw new Exception("Expecting exactly 1 rotor in " + ROTOR_REFERENCE_GROUP);
        }

        return rotors[0];
    }

    private void UpdateMinError(ZACommons commons)
    {
        MinError = BaseRotorStep * Math.Pow(10.0, StepFactor);
        var rotor = GetRotor(commons);
        SetPoint = rotor.Angle;
    }

    private IMyCubeBlock GetReference(ZACommons commons, string groupName)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group == null)
        {
            throw new Exception("Missing group: " + groupName);
        }
        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(group.Blocks);
        if (controllers.Count == 0)
        {
            throw new Exception("Expecting at least 1 ship controller in " + groupName);
        }
        return controllers[0];
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        pid.Kp = RotorKp;
        pid.Ki = RotorKi;
        pid.Kd = RotorKd;

        UpdateMinError(commons);

        eventDriver.Schedule(0, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument, Action<ZACommons, Vector3D> targetAction)
    {
        argument = argument.Trim().ToLower();
        switch (argument)
        {
            case "left":
                SetPoint -= MinError;
                break;
            case "right":
                SetPoint += MinError;
                break;
            case "reset":
                SetPoint = 0.0;
                break;
            case "factorup":
                StepFactor++;
                UpdateMinError(commons);
                break;
            case "factordown":
                StepFactor--;
                UpdateMinError(commons);
                break;
            case "factorreset":
                StepFactor = -1;
                UpdateMinError(commons);
                break;
            case "compute":
                var firstReference = GetReference(commons, STATIC_REFERENCE_GROUP);
                var rotorReference = GetReference(commons, ROTOR_REFERENCE_GROUP);

                var first = new Rangefinder.LineSample(firstReference);
                var second = new Rangefinder.LineSample(rotorReference);
                Vector3D closestFirst, closestSecond;
                if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
                {
                    // Take midpoint of closestFirst-closestSecond segment
                    var target = (closestFirst + closestSecond) / 2.0;
                    targetAction(commons, target);
                }
                break;
            default:
                break;
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var rotor = GetRotor(commons);

        //commons.Echo("Angle: " + rotor.Angle);
        //commons.Echo("SetPoint: " + SetPoint);
        var error = SetPoint - rotor.Angle;
        if (error > Math.PI) error -= Math.PI * 2.0;
        if (error < -Math.PI) error += Math.PI * 2.0;
        //commons.Echo("Error: " + error);

        if (Math.Abs(error) > MinError / 2.0)
        {
            var rotorVelocity = pid.Compute(error);
            rotor.SetValue<float>("Velocity", (float)rotorVelocity);
        }
        else
        {
            rotor.SetValue<float>("Velocity", 0.0f);
        }

        eventDriver.Schedule(TicksPerRun, Run);
    }
}
