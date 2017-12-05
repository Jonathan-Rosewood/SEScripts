//@ commons eventdriver
public class AirVentManager
{
    private const double RunDelay = 3.0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(1, Tick);
    }

    private List<IMyAirVent> GetAirVents(ZACommons commons)
    {
        return ZACommons.GetBlocksOfType<IMyAirVent>(commons.AllBlocks,
                                                     vent => vent.IsFunctional &&
                                                     vent.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0 &&
                                                     vent.CustomName.IndexOf("[Intake]", ZACommons.IGNORE_CASE) < 0);
    }

    public void Tick(ZACommons commons, EventDriver eventDriver)
    {
        // Enable all manageable air vents for 1 tick before performing checks.
        // Necessary as of 1.185. Thanks, Keen!
        var vents = GetAirVents(commons);
        vents.ForEach(vent =>
                {
                    vent.Enabled = true;
                });

        eventDriver.Schedule(1, Tock);
    }

    public void Tock(ZACommons commons, EventDriver eventDriver)
    {
        var vents = GetAirVents(commons);
        vents.ForEach(vent =>
                {
                    var level = vent.GetOxygenLevel();
                    if (vent.Depressurize && level > 0.0f)
                    {
                        vent.Enabled = true;
                    }
                    else if (!vent.Depressurize)
                    {
                        if (level < MIN_AIR_VENT_PRESSURE)
                        {
                            vent.Enabled = true;
                        }
                        else if (level > MAX_AIR_VENT_PRESSURE)
                        {
                            vent.Enabled = false;
                        }
                    }
                });

        eventDriver.Schedule(RunDelay, Tick);
    }
}
