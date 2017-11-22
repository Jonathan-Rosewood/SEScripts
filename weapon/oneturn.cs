//@ shipcontrol eventdriver basemissileguidance seeker
public class OneTurn : BaseMissileGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Action<ZACommons, EventDriver> NextStage;

    private TimeSpan OneTurnEnd;
    public bool Turned { get; private set; }

    public OneTurn()
    {
        Turned = false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> nextStage)
    {
        NextStage = nextStage;
        OneTurnEnd = eventDriver.TimeSinceStart + TimeSpan.FromSeconds(ONE_TURN_DURATION);

        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Interpolate position since last update
        var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
        var targetGuess = TargetAimPoint + TargetVelocity * delta.TotalSeconds;

        var targetVector = targetGuess - shipControl.ReferencePoint;

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector, out yawError, out pitchError);

        if (OneTurnEnd < eventDriver.TimeSinceStart)
        {
            Turned = true;
            NextStage(commons, eventDriver);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }

    public override void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        // Check if already Turned. Don't waste cycles parsing commands.
        if (!Turned)
        {
            base.HandleCommand(commons, eventDriver, argument);
        }
    }
}
