//@ commons eventdriver
public class Stocker
{
    private const double RunDelay = 2.0;
    private char COUNT_DELIMITER = ':';

    public struct MissingStock
    {
        public IMyInventory Inventory;
        public VRage.MyFixedPoint Amount;

        public MissingStock(IMyInventory inventory, VRage.MyFixedPoint amount)
        {
            Inventory = inventory;
            Amount = amount;
        }
    }
        
    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(RunDelay, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        _Run(commons, eventDriver);
        eventDriver.Schedule(RunDelay, Run);
    }

    private void _Run(ZACommons commons, EventDriver eventDriver)
    {
        // No source, don't bother
        var stockGroup = commons.GetBlockGroupWithName(STOCKER_SOURCE_NAME);
        if (stockGroup == null) return;

        var toCheck = new Dictionary<IMyTerminalBlock, Dictionary<string, int>>();

        foreach (var group in commons.GetBlockGroupsWithPrefix(STOCKER_PREFIX))
        {
            if (group.Name == STOCKER_SOURCE_NAME) continue;

            // Determine count
            var parts = group.Name.Split(new char[] { COUNT_DELIMITER }, 2);
            var count = 1;
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out count))
                {
                    count = Math.Max(count, 1);
                }
                else
                {
                    count = 1;
                }
            }

            // Determine SubtypeName
            var subtypeName = parts[0].Substring(STOCKER_PREFIX.Length).Trim();
            if (subtypeName.Length == 0) continue;

            // Gather destinations and wanted subtypes/counts
            group.Blocks.ForEach(block => {
                    if (!block.IsFunctional || block.GetInventoryCount() == 0) return;

                    Dictionary<string, int> wantedStocks;
                    if (!toCheck.TryGetValue(block, out wantedStocks))
                    {
                        wantedStocks = new Dictionary<string, int>();
                        toCheck.Add(block, wantedStocks);
                    }

                    int wanted;
                    if (wantedStocks.TryGetValue(subtypeName, out wanted))
                    {
                        // Use biggest request
                        wanted = Math.Max(wanted, count);
                    }
                    else
                    {
                        wanted = count;
                    }

                    wantedStocks[subtypeName] = wanted;
                });
        }

        // Determine how many are missing from all destinations
        var missingStocks = new Dictionary<string, LinkedList<MissingStock>>();
        foreach (var kv in toCheck)
        {
            var block = kv.Key;
            var wantedStocks = kv.Value;

            // Gather current item counts
            var currents = new Dictionary<string, VRage.MyFixedPoint>();
            for (int i = 0; i < block.GetInventoryCount(); i++)
            {
                var inventory = block.GetInventory(i);
                var items = inventory.GetItems();
                items.ForEach(item => {
                        // Only care about wanted items for this block
                        var subtypeName = item.Content.SubtypeName;
                        if (wantedStocks.ContainsKey(subtypeName))
                        {
                            VRage.MyFixedPoint current;
                            if (!currents.TryGetValue(subtypeName, out current))
                            {
                                current = (VRage.MyFixedPoint)0.0f;
                            }
                            current += item.Amount;

                            currents[subtypeName] = current;
                        }
                    });
            }

            // Now figure out what's missing
            foreach (var kv2 in wantedStocks)
            {
                var subtypeName = kv2.Key;
                var count = kv2.Value;

                VRage.MyFixedPoint current;
                if (!currents.TryGetValue(subtypeName, out current))
                {
                    current = (VRage.MyFixedPoint)0.0f;
                }

                if (current < count)
                {
                    // Add to dictionary under SubtypeName & keep track
                    // of this block
                    LinkedList<MissingStock> missing;
                    if (!missingStocks.TryGetValue(subtypeName, out missing))
                    {
                        missing = new LinkedList<MissingStock>();
                        missingStocks.Add(subtypeName, missing);
                    }

                    // NB Assumes first inventory
                    missing.AddLast(new MissingStock(block.GetInventory(0), (VRage.MyFixedPoint)count - current));
                }
            }
        }

        // Nothing missing, nothing to do
        if (missingStocks.Count == 0) return;

        // Now attempt to fill missing blocks
        stockGroup.Blocks.ForEach(source => {
                if (!source.IsFunctional) return;

                for (int i = 0; i < source.GetInventoryCount(); i++)
                {
                    var inventory = source.GetInventory(i);
                    var items = inventory.GetItems();
                    for (int j = items.Count - 1; j >= 0; j--)
                    {
                        var item = items[j];
                        var subtypeName = item.Content.SubtypeName;
                        LinkedList<MissingStock> missing;
                        if (missingStocks.TryGetValue(subtypeName, out missing))
                        {
                            var sourceAmount = item.Amount;
                            while (missing.First != null &&
                                   sourceAmount > (VRage.MyFixedPoint)0.0f)
                            {
                                var dest = missing.First.Value;
                                VRage.MyFixedPoint transferAmount;
                                // Has enough to fully restock?
                                if (sourceAmount >= dest.Amount)
                                {
                                    transferAmount = dest.Amount;
                                    // Assume it will succeed
                                    missing.RemoveFirst();
                                }
                                else
                                {
                                    transferAmount = sourceAmount;
                                    dest.Amount -= transferAmount;
                                }
                                // Move some over
                                dest.Inventory.TransferItemFrom(inventory, j, stackIfPossible: true, amount: transferAmount);
                                // FIXME no error checking

                                sourceAmount -= transferAmount;
                            }
                        }
                    }
                }
            });
    }
}
