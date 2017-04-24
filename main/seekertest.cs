//! Seeker Test
//@ shipcontrol eventdriver seeker
private readonly EventDriver eventDriver = new EventDriver();
private readonly SeekerTest seekerTest = new SeekerTest();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "Reference");

        seekerTest.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
        },
        postAction: () => {
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

public class SeekerTest
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        shipControl.GyroControl.Reset();
        shipControl.GyroControl.EnableOverride(true);
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var targetVector = Vector3D.Normalize(TARGET_POINT - shipControl.ReferencePoint);

        double yawError, pitchError, rollError;
        if (ROLL_TOO)
        {
            seeker.Seek(shipControl, targetVector, TARGET_UP, out yawError, out pitchError, out rollError);
        }
        else
        {
            seeker.Seek(shipControl, targetVector, out yawError, out pitchError);
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
