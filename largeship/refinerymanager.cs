public class RefineryManager
{
    public struct RefineryWrapper : IComparable<RefineryWrapper>
    {
        public IMyRefinery Refinery;
        public VRage.ModAPI.IMyInventory Inventory;
        public VRage.ModAPI.IMyInventoryItem Item;
        public float Amount;

        public RefineryWrapper(IMyRefinery refinery)
        {
            Refinery = refinery;
            Inventory = refinery.GetInventory(0);
            var items = Inventory.GetItems();
            Item = items.Count > 0 ? items[0] : null;
            Amount = Item != null ? (float)Item.Amount : 0.0f;
        }

        public int CompareTo(RefineryWrapper other)
        {
            return other.Amount.CompareTo(Amount);
        }
    }

    private const double RunDelay = 1.0;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var refineries = ZACommons
            .GetBlocksOfType<IMyRefinery>(commons.Blocks,
                                          block => block.IsFunctional &&
                                          block.IsWorking &&
                                          ((IMyRefinery)block).Enabled &&
                                          ((IMyRefinery)block).UseConveyorSystem);

        var isProducing = false;
        var isIdle = false;
        var wrappers = new LinkedList<RefineryWrapper>();
        for (var e = refineries.GetEnumerator(); e.MoveNext();)
        {
            var refinery = (IMyRefinery)e.Current;
            InsertSorted(wrappers, new RefineryWrapper(refinery));
            if (refinery.IsProducing)
            {
                isProducing = true;
            }
            else
            {
                isIdle = true;
            }
        }

        if (isProducing && isIdle)
        {
            var first = wrappers.First.Value;
            var last = wrappers.Last.Value;
            if (last.Amount == 0.0f)
            {
                // Take half from the first
                VRage.MyFixedPoint amount = first.Item.Amount * (VRage.MyFixedPoint)0.5f;
                // And move it to the last
                first.Inventory.TransferItemTo(last.Inventory, 0, amount: amount);
            }
        }

        eventDriver.Schedule(RunDelay, Run);
    }

    // Because Keen broke List.Sort() and Array.Sort() some time ago...
    private void InsertSorted(LinkedList<RefineryWrapper> wrappers,
                              RefineryWrapper wrapper)
    {
        // Just do insertion sort
        for (var current = wrappers.First;
             current != null;
             current = current.Next)
        {
            if (wrapper.CompareTo(current.Value) < 0)
            {
                // Insert before this one
                wrappers.AddBefore(current, wrapper);
                return;
            }
        }
        // Just add at the end
        wrappers.AddLast(wrapper);
    }
}
