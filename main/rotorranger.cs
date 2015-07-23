private const int TicksPerRun = 5;
private const double RunsPerSecond = 60.0 / TicksPerRun;

private readonly EventDriver eventDriver = new EventDriver();
private readonly RotorController rotorController = new RotorController();

private bool FirstRun = true;

private const double BASE_ROTOR_STEP = Math.PI / 180.0;
private int StepFactor = -1;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        rotorController.Init(this, eventDriver);
        rotorController.MinError = BASE_ROTOR_STEP * Math.Pow(10.0, StepFactor);
    }
        
    eventDriver.Tick(this);

    argument = argument.Trim().ToLower();
    switch (argument)
    {
        case "left":
            rotorController.SetPoint -= rotorController.MinError;
            break;
        case "right":
            rotorController.SetPoint += rotorController.MinError;
            break;
        case "factorup":
            StepFactor++;
            rotorController.MinError = BASE_ROTOR_STEP * Math.Pow(10.0, StepFactor);
            break;
        case "factordown":
            StepFactor--;
            rotorController.MinError = BASE_ROTOR_STEP * Math.Pow(10.0, StepFactor);
            break;
        case "reset":
            rotorController.SetPoint = 0.0;
            break;
        case "compute":
            var staticGroup = ZALibrary.GetBlockGroupWithName(this, "Static Reference");
            var firstReferences = ZALibrary.GetBlocksOfType<IMyShipController>(staticGroup.Blocks);
            var firstReference = firstReferences[0];

            var rotorGroup = ZALibrary.GetBlockGroupWithName(this, "Rotor Reference");
            var rotorReferences = ZALibrary.GetBlocksOfType<IMyShipController>(rotorGroup.Blocks);
            var rotorReference = rotorReferences[0];

            var first = new Rangefinder.LineSample(firstReference);
            var second = new Rangefinder.LineSample(rotorReference);
            Vector3D closestFirst, closestSecond;
            if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
            {
                var target = (closestFirst + closestSecond) / 2.0;

                var targetGroup = ZALibrary.GetBlockGroupWithName(this, RANGEFINDER_TARGET_GROUP);
                if (targetGroup != null)
                {
                    var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                                     target.GetDim(0),
                                                     target.GetDim(1),
                                                     target.GetDim(2));

                    for (var e = ZALibrary.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
                    {
                        e.Current.WritePublicText(targetString);
                    }
                }
            }
            break;
        default:
            break;
    }
}

public class RotorController
{
    private const double RotorKp = 100.0;
    private const double RotorKi = 0.0;
    private const double RotorKd = 0.0;

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

    private IMyMotorStator Rotor;

    public double MinError
    {
        get { return m_minError; }
        set
        {
            m_minError = value;
            SetPoint = Rotor.Angle;
        }
    }
    private double m_minError;

    public void Init(MyGridProgram program, EventDriver eventDriver)
    {
        pid.Kp = RotorKp;
        pid.Ki = RotorKi;
        pid.Kd = RotorKd;

        var rotorGroup = ZALibrary.GetBlockGroupWithName(program, "Rotor Reference");
        var rotors = ZALibrary.GetBlocksOfType<IMyMotorStator>(rotorGroup.Blocks);
        Rotor = rotors[0];

        eventDriver.Schedule(0, Run);
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        //program.Echo("Angle: " + Rotor.Angle);
        //program.Echo("SetPoint: " + SetPoint);
        var error = SetPoint - Rotor.Angle;
        if (error > Math.PI) error -= Math.PI * 2.0;
        if (error < -Math.PI) error += Math.PI * 2.0;
        //program.Echo("Error: " + error);

        if (Math.Abs(error) > MinError / 2.0)
        {
            var rotorVelocity = pid.Compute(error);
            Rotor.SetValue<float>("Velocity", (float)rotorVelocity);
        }
        else
        {
            Rotor.SetValue<float>("Velocity", 0.0f);
        }

        eventDriver.Schedule(TicksPerRun, Run);
    }
}
