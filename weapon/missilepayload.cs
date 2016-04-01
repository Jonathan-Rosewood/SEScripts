//@ commons eventdriver
public class MissilePayload
{
    private const string PAYLOAD_GROUP = "CM Payload";

    private const int TicksPerRun = 1;

    private const float StackSize = 10000.0f;

    public bool SplitContainerContents(IMyCargoContainer container)
    {
        var inventory = ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)container).GetInventory(0);
        var items = inventory.GetItems();
        // NB We only check the first item
        var item = items.Count > 0 ? items[0] : null;
        var amount = item != null ? (float)item.Amount : 0.0f;
        if (amount >= StackSize * 2.0f)
        {
            // Move to new stack
            VRage.MyFixedPoint newStackAmount = (VRage.MyFixedPoint)StackSize;
            inventory.TransferItemTo(inventory, 0, targetItemIndex: items.Count,
                                     stackIfPossible: false, amount: newStackAmount);
            return true;
        }
        return false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        // Bleah, might be merged, so use a group
        var payloadGroup = commons.GetBlockGroupWithName(PAYLOAD_GROUP + MISSILE_GROUP_SUFFIX);
        if (payloadGroup == null) return;
        var containers = ZACommons.GetBlocksOfType<IMyCargoContainer>(payloadGroup.Blocks);
        if (containers.Count == 0) return;

        // Leisurely pace of one stack per container per frame
        bool moved = false;
        for (var e = containers.GetEnumerator(); e.MoveNext();)
        {
            var container = e.Current;
            if (SplitContainerContents(container)) moved = true;
        }

        if (moved) eventDriver.Schedule(1, Run);
    }
}
