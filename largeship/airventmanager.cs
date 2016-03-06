public class AirVentManager
{
    private const double RunDelay = 1.0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var vents = ZACommons.GetBlocksOfType<IMyAirVent>(commons.AllBlocks,
                                                          vent => vent.IsFunctional &&
                                                          vent.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0 &&
                                                          vent.CustomName.IndexOf("[Intake]", ZACommons.IGNORE_CASE) < 0);

        vents.ForEach(block =>
                {
                    var vent = (IMyAirVent)block;
                    var level = vent.GetOxygenLevel();
                    if (vent.IsDepressurizing && !vent.Enabled && level > 0.0f)
                    {
                        vent.SetValue<bool>("OnOff", true);
                    }
                    else if (!vent.IsDepressurizing)
                    {
                        if (level < MIN_AIR_VENT_PRESSURE && !vent.Enabled)
                        {
                            vent.SetValue<bool>("OnOff", true);
                        }
                        else if (level > MAX_AIR_VENT_PRESSURE && vent.Enabled)
                        {
                            vent.SetValue<bool>("OnOff", false);
                        }
                    }
                });

        eventDriver.Schedule(RunDelay, Run);
    }
}
