//@ commons eventdriver
// Note that if there are no tanks, the Low Oxygen timer block is triggered
// on compile/world load. For now, this is desired.
public class OxygenManager
{
    private const double RunDelay = 1.0;

    private const int OXYGEN_LEVEL_HIGH = 2;
    private const int OXYGEN_LEVEL_NORMAL = 1;
    private const int OXYGEN_LEVEL_BUFFER = 0;
    private const int OXYGEN_LEVEL_LOW = -1;
    private const int OXYGEN_LEVEL_UNKNOWN = -2; // Only used for first run

    private int PreviousState = OXYGEN_LEVEL_UNKNOWN;

    private float GetAverageOxygenTankLevel(List<IMyOxygenTank> tanks)
    {
        float total = 0.0f;
        int count = 0;

        foreach (var tank in tanks)
        {
            total += tank.GetOxygenLevel();
            count++;
        }

        return count != 0 ? total / count : 0.0f;
    }

    private int GetOxygenState(List<IMyOxygenTank> tanks)
    {
        var level = GetAverageOxygenTankLevel(tanks);
        if (level >= MAX_OXYGEN_TANK_LEVEL)
        {
            return OXYGEN_LEVEL_HIGH;
        }
        else if (level >= MIN_OXYGEN_TANK_LEVEL)
        {
            return OXYGEN_LEVEL_NORMAL;
        }
        else if (level > LOW_OXYGEN_TANK_LEVEL) 
        {
            return OXYGEN_LEVEL_BUFFER;
        }
        else
        {
            return OXYGEN_LEVEL_LOW;
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var tanks = ZACommons.GetBlocksOfType<IMyOxygenTank>(commons.AllBlocks,
                                                             tank => tank.IsFunctional &&
                                                             tank.IsWorking &&
                                                             tank.CustomName.IndexOf("[Excluded]", ZACommons.IGNORE_CASE) < 0);

        var currentState = GetOxygenState(tanks);

        // Only act on level transitions
        if (PreviousState != currentState)
        {
            PreviousState = currentState;

            // We need a tri-state variable, so... nullable
            bool? generateOxygen = null;
            bool? farmOxygen = null;

            switch (currentState)
            {
                case OXYGEN_LEVEL_HIGH:
                    // Turn off all oxygen production
                    generateOxygen = false;
                    farmOxygen = false;
                    break;
                case OXYGEN_LEVEL_NORMAL:
                    // Do nothing (but keep farms up)
                    farmOxygen = true;
                    break;
                case OXYGEN_LEVEL_BUFFER:
                    // Start producing oxygen
                    generateOxygen = true;
                    farmOxygen = true;
                    break;
                case OXYGEN_LEVEL_LOW:
                    // Definitely start producing oxygen
                    generateOxygen = true;
                    farmOxygen = true;
                    // For now, it's intentional that we start timer blocks
                    // on all grids... we'll see how it goes
                    ZACommons.StartTimerBlockWithName(commons.AllBlocks, LOW_OXYGEN_NAME);
                    break;
            }

            if (generateOxygen != null)
            {
                // Limit to this grid -- don't mess with any other ship's systems
                var generators =
                    ZACommons.GetBlocksOfType<IMyOxygenGenerator>(commons.Blocks,
                                                                  block => block.IsFunctional);
                ZACommons.EnableBlocks(generators, (bool)generateOxygen);
            }
            if (farmOxygen != null)
            {
                var farms =
                    ZACommons.GetBlocksOfType<IMyOxygenFarm>(commons.Blocks,
                                                             block => block.IsFunctional);

                // Farms don't implement IMyFunctionalBlock??
                ZACommons.EnableBlocks(farms, (bool)farmOxygen);

                // We'll count atmosphere intake vents too, since they're "free"
                var vents =
                    ZACommons.GetBlocksOfType<IMyAirVent>(commons.Blocks,
                                                          vent => vent.IsFunctional &&
                                                          vent.IsDepressurizing &&
                                                          vent.CustomName.IndexOf("[Intake]", ZACommons.IGNORE_CASE) >= 0);

                ZACommons.EnableBlocks(vents, (bool)farmOxygen);
            }
        }

        eventDriver.Schedule(RunDelay, Run);
    }
}
