public class OxygenManager
{
    private const int OXYGEN_LEVEL_HIGH = 2;
    private const int OXYGEN_LEVEL_NORMAL = 1;
    private const int OXYGEN_LEVEL_BUFFER = 0;
    private const int OXYGEN_LEVEL_LOW = -1;
    private const int OXYGEN_LEVEL_UNKNOWN = -2; // Only used for first run

    private bool FirstRun = true;
    private int PreviousState;

    private float GetAverageOxygenTankLevel(List<IMyOxygenTank> blocks)
    {
        float total = 0.0f;
        int count = 0;

        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            if (block is IMyOxygenTank)
            {
                total += ((IMyOxygenTank)block).GetOxygenLevel();
                count++;
            }
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

    public void Run(MyGridProgram program, List<IMyTerminalBlock> ship)
    {
        var tanks = ZALibrary.GetBlocksOfType<IMyOxygenTank>(ship);

        var currentState = GetOxygenState(tanks);

        if (FirstRun)
        {
            FirstRun = false;
            PreviousState = OXYGEN_LEVEL_UNKNOWN;
        }

        // Only act on level transitions
        if (PreviousState != currentState)
        {
            PreviousState = currentState;

            // We need a tri-state variable, so... nullable
            bool? produceOxygen = null;

            switch (currentState)
            {
                case OXYGEN_LEVEL_HIGH:
                    // Turn off oxygen production
                    produceOxygen = false;
                    break;
                case OXYGEN_LEVEL_NORMAL:
                    // Do nothing
                    break;
                case OXYGEN_LEVEL_BUFFER:
                    // Start producing oxygen
                    produceOxygen = true;
                    break;
                case OXYGEN_LEVEL_LOW:
                    // Definitely start producing oxygen
                    produceOxygen = true;
                    // For now, it's intentional that we start timer blocks
                    // on all grids... we'll see how it goes
                    ZALibrary.StartTimerBlockWithName(ship, LOW_OXYGEN_NAME);
                    break;
            }

            if (produceOxygen != null)
            {
                // Limit to this grid -- don't mess with any other ship's systems
                var generators =
                    ZALibrary.GetBlocksOfType<IMyOxygenGenerator>(ship,
                                                                  delegate (IMyOxygenGenerator block)
                                                                  {
                                                                      return block.CubeGrid == program.Me.CubeGrid;
                                                                  });
                ZALibrary.EnableBlocks(generators, (bool)produceOxygen);

                var farms =
                    ZALibrary.GetBlocksOfType<IMyOxygenFarm>(ship,
                                                             delegate (IMyOxygenFarm block)
                                                             {
                                                                 return block.CubeGrid == program.Me.CubeGrid;
                                                             });

                // Farms don't implement IMyFunctionalBlock??
                ZALibrary.EnableBlocks(farms, (bool)produceOxygen);
            }
        }
    }
}
