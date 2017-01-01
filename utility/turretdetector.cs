//@ commons eventdriver
public class TurretBasedDetector
{
    private const double RunDelay = 1.0;

    public struct TurretInfo
    {
        public float LastElevation;
        public float LastAzimuth;

        public TurretInfo(IMyLargeTurretBase turret)
        {
            LastElevation = turret.Elevation;
            LastAzimuth = turret.Azimuth;
        }
    }

    private readonly Dictionary<IMyLargeTurretBase, TurretInfo> turretInfos = new Dictionary<IMyLargeTurretBase, TurretInfo>();
    private bool Triggered = false;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!Triggered)
        {
            var turretGroup = commons.GetBlockGroupWithName(TURRET_DETECTOR_GROUP);
            if (turretGroup != null)
            {
                var turrets = ZACommons.GetBlocksOfType<IMyLargeTurretBase>(turretGroup.Blocks,
                                                                            block => block.CubeGrid == commons.Me.CubeGrid);
                foreach (var turret in turrets)
                {
                    TurretInfo info;
                    if (turretInfos.TryGetValue(turret, out info))
                    {
                        if (turret.Elevation != info.LastElevation ||
                            turret.Azimuth != info.LastAzimuth)
                        {
                            // Trigger
                            ZACommons.StartTimerBlockWithName(commons.Blocks,
                                                              TURRET_DETECTOR_TRIGGER_TIMER_BLOCK_NAME);
                            // And don't trigger again until reset
                            Triggered = true;
                            break;
                        }
                    }
                    else
                    {
                        // Unknown turret
                        // FIXME shouldn't hold references...
                        turretInfos.Add(turret, new TurretInfo(turret));
                    }
                }
            }
        }

        eventDriver.Schedule(RunDelay, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.ToLower().Trim();
        if (argument == "reset")
        {
            turretInfos.Clear();
            Triggered = false;
        }
    }
}
