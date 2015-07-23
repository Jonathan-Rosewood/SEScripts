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

    private IMyMotorStator GetRotor(MyGridProgram program)
    {
        var rotorGroup = ZALibrary.GetBlockGroupWithName(program, ROTOR_REFERENCE_GROUP);
        if (rotorGroup == null)
        {
            throw new Exception("Missing group: " + ROTOR_REFERENCE_GROUP);
        }
        var rotors = ZALibrary.GetBlocksOfType<IMyMotorStator>(rotorGroup.Blocks);
        if (rotors.Count != 1)
        {
            throw new Exception("Expecting exactly 1 rotor in " + ROTOR_REFERENCE_GROUP);
        }

        return rotors[0];
    }

    private void UpdateMinError(MyGridProgram program)
    {
        MinError = BaseRotorStep * Math.Pow(10.0, StepFactor);
        var rotor = GetRotor(program);
        SetPoint = rotor.Angle;
    }

    private IMyCubeBlock GetReference(MyGridProgram program, string groupName)
    {
        var group = ZALibrary.GetBlockGroupWithName(program, groupName);
        if (group == null)
        {
            throw new Exception("Missing group: " + groupName);
        }
        var controllers = ZALibrary.GetBlocksOfType<IMyShipController>(group.Blocks);
        if (controllers.Count == 0)
        {
            throw new Exception("Expecting at least 1 ship controller in " + groupName);
        }
        return controllers[0];
    }

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        pid.Kp = RotorKp;
        pid.Ki = RotorKi;
        pid.Kd = RotorKd;

        UpdateMinError(program);

        eventDriver.Schedule(0, Run);
    }

    public void HandleCommand(MyGridProgram program, EventDriver eventDriver,
                              string argument, Action<Vector3D> targetAction)
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
                UpdateMinError(program);
                break;
            case "factordown":
                StepFactor--;
                UpdateMinError(program);
                break;
            case "factorreset":
                StepFactor = -1;
                UpdateMinError(program);
                break;
            case "compute":
                var firstReference = GetReference(program, STATIC_REFERENCE_GROUP);
                var rotorReference = GetReference(program, ROTOR_REFERENCE_GROUP);

                var first = new Rangefinder.LineSample(firstReference);
                var second = new Rangefinder.LineSample(rotorReference);
                Vector3D closestFirst, closestSecond;
                if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
                {
                    // Take midpoint of closestFirst-closestSecond segment
                    var target = (closestFirst + closestSecond) / 2.0;
                    targetAction(target);
                }
                break;
            default:
                break;
        }
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        var rotor = GetRotor(program);

        //program.Echo("Angle: " + rotor.Angle);
        //program.Echo("SetPoint: " + SetPoint);
        var error = SetPoint - rotor.Angle;
        if (error > Math.PI) error -= Math.PI * 2.0;
        if (error < -Math.PI) error += Math.PI * 2.0;
        //program.Echo("Error: " + error);

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
