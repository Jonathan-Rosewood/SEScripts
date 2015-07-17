public class RefineryManager
{
    public struct RefineryWrapper
    {
        public IMyRefinery Refinery;
        public IMyInventory Inventory;
        public IMyInventoryItem Item;
        public float Amount;

        public RefineryWrapper(IMyRefinery refinery)
        {
            Refinery = refinery;
            Inventory = ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)refinery).GetInventory(0);
            var items = Inventory.GetItems();
            Item = items.Count > 0 ? items[0] : null;
            Amount = Item != null ? (float)Item.Amount : 0.0f;
        }
    }

    public class RefineryComparer : IComparer<RefineryWrapper>
    {
        public int Compare(RefineryWrapper a, RefineryWrapper b)
        {
            return b.Amount.CompareTo(a.Amount);
        }
    }

    private readonly RefineryComparer refineryComparer = new RefineryComparer();

    public void Run(MyGridProgram program, List<IMyTerminalBlock> ship)
    {
        var refineries =
            ZALibrary.GetBlocksOfType<IMyRefinery>(ship,
                                                   block => block.CubeGrid == program.Me.CubeGrid &&
                                                   block.IsFunctional &&
                                                   block.IsWorking &&
                                                   block.Enabled &&
                                                   block.UseConveyorSystem);

        var isProducing = false;
        var isIdle = false;
        var wrappers = new List<RefineryWrapper>();
        for (var e = refineries.GetEnumerator(); e.MoveNext();)
        {
            var refinery = e.Current;
            wrappers.Add(new RefineryWrapper(refinery));
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
            wrappers.Sort(refineryComparer);
            var first = wrappers[0];
            var last = wrappers[wrappers.Count - 1];
            if (last.Amount == 0.0f)
            {
                // Take half from the first
                VRage.MyFixedPoint amount = first.Item.Amount * (VRage.MyFixedPoint)0.5f;
                // And move it to the last
                first.Inventory.TransferItemTo(last.Inventory, 0, amount: amount);
            }
        }
    }
}
