//@ shipcontrol eventdriver
public class GuidanceKill
{
    private const uint FramesPerRun = 1;

    private readonly List<IMyCubeBlock> BlocksToCheck = new List<IMyCubeBlock>();
    private Vector3D StartPoint;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        commons.Blocks.ForEach(block =>
                {
                    if (block is IMyProgrammableBlock ||
                        block is IMyTimerBlock)
                    {
                        BlocksToCheck.Add(block);
                    }
                });
        StartPoint = ((ShipControlCommons)commons).ReferencePoint;
        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        bool kill = false;
        // Check if any essential blocks have been damaged
        foreach (var block in BlocksToCheck)
        {
            var slimBlock = block.CubeGrid.GetCubeBlock(block.Position);
            if (slimBlock.CurrentDamage > 0.0f)
            {
                kill = true;
                break;
            }
        }

        // Check distance from start point
        if (!kill)
        {
            var distance = (((ShipControlCommons)commons).ReferencePoint - StartPoint).Length();
            kill = distance >= KILL_DISTANCE;
        }

        if (kill)
        {
            var shipControl = (ShipControlCommons)commons;
            shipControl.GyroControl.EnableOverride(false);
            shipControl.ThrustControl.Enable(false);
            // Shut down all timer blocks
            ZACommons.ForEachBlockOfType<IMyTimerBlock>(commons.Blocks, block =>
                    {
                        block.SetValue<bool>("OnOff", false);
                    });
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }
}
