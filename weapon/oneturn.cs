//@ shipcontrol eventdriver seeker
public class OneTurn
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Action<ZACommons, EventDriver> NextStage;

    private Vector3D Target;

    private TimeSpan OneTurnEnd;

    public void AcquireTarget(ZACommons commons)
    {
        // Find the sole text panel
        var panelGroup = commons.GetBlockGroupWithName("CM Target");
        if (panelGroup == null)
        {
            throw new Exception("Missing group: CM Target");
        }

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0)
        {
            throw new Exception("Expecting at least 1 text panel");
        }
        var panel = panels[0]; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 6)
        {
            throw new Exception("Expecting exactly 6 parts to target info");
        }
        Target = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            Target.SetDim(i, double.Parse(parts[i]));
        }
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

        var targetVector = Target - shipControl.ReferencePoint;

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector, out yawError, out pitchError);

        if (OneTurnEnd < eventDriver.TimeSinceStart)
        {
            NextStage(commons, eventDriver);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }
}
