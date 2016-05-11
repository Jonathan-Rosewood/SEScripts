//! Smart Shell Controller
//@ shipcontrol eventdriver dumbshell
private readonly EventDriver eventDriver = new EventDriver(timerName: "ShellClock");
private readonly SmartShell smartShell = new SmartShell();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "CannonReference");

        smartShell.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons);
}

public class SmartShell
{
    private const uint TicksPerRun = 1;
    private const double RunsPerSecond = 60.0 / TicksPerRun;

    private readonly DumbShell dumbShell = new DumbShell();

    private Vector3D Target;
    private bool Decoy1Released = false;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var target = AcquireTarget(commons);
        // Use current distance from target point, otherwise use default range
        var range = target != null ?
            ((Vector3D)target - shipControl.ReferencePoint).Length() :
            DefaultRange;
        // Target point is directly in front
        Target = shipControl.ReferencePoint + shipControl.ReferenceForward * range;

        dumbShell.Init(commons, eventDriver, postLaunch: DecoyLoop);
    }

    public void DecoyLoop(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var distance = (Target - shipControl.ReferencePoint).LengthSquared();

        // FIXME Need to do this better
        if (distance < Decoy2Distance * Decoy2Distance)
        {
            // Release decoy #2
            LaunchDecoy(commons, eventDriver, " #2");
            return; // We're done
        }
        else if (!Decoy1Released && distance < Decoy1Distance * Decoy1Distance)
        {
            // Release decoy #1
            LaunchDecoy(commons, eventDriver, " #1");
            Decoy1Released = true;
        }

        eventDriver.Schedule(TicksPerRun, DecoyLoop);
    }

    // Acquire target from CM Target text panel. If anything's wrong,
    // return null.
    private Vector3D? AcquireTarget(ZACommons commons)
    {
        // Find the sole text panel
        var panelGroup = commons.GetBlockGroupWithName("CM Target");
        if (panelGroup == null) return null;

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0) return null;
        var panel = panels[0] as IMyTextPanel; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 3) return null;

        var target = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            double coord;
            if (!double.TryParse(parts[i], out coord)) return null;
            target.SetDim(i, coord);
        }

        return target;
    }

    private void LaunchDecoy(ZACommons commons, EventDriver eventDriver,
                             string suffix)
    {
        var group = commons.GetBlockGroupWithName("SS Decoy Set" + suffix);
        // Activate decoys
        ZACommons.ForEachBlockOfType<IMyTerminalBlock>(group.Blocks,
                                                       block =>
                {
                    if (block.DefinitionDisplayNameText == "Decoy")
                    {
                        block.SetValue<bool>("OnOff", true);
                    }
                });
        eventDriver.Schedule(DecoyReleaseDelay, (c, e) => {
                var g = c.GetBlockGroupWithName("SS Decoy Set" + suffix);
                // Deactivate merge block
                ZACommons.ForEachBlockOfType<IMyShipMergeBlock>(g.Blocks,
                                                                merge =>
                        {
                            merge.SetValue<bool>("OnOff", false);
                        });
            });
    }
}
