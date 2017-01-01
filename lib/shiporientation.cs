//@ commons
public class ShipOrientation
{
    public Base6Directions.Direction ShipUp { get; private set; }
    public Base6Directions.Direction ShipForward { get; private set; }
    public MyBlockOrientation BlockOrientation
    {
        get
        {
            return new MyBlockOrientation(ShipForward, ShipUp);
        }
    }

    public ShipOrientation()
    {
        // Defaults
        ShipUp = Base6Directions.Direction.Up;
        ShipForward = Base6Directions.Direction.Forward;
    }

    // Use orientation of given block
    public void SetShipReference(IMyCubeBlock reference)
    {
        ShipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
    }

    // Use orientation of a block in the given group
    public void SetShipReference(ZACommons commons, string groupName,
                                 Func<IMyTerminalBlock, bool> condition = null)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (block.CubeGrid == commons.Me.CubeGrid &&
                    (condition == null || condition(block)))
                {
                    SetShipReference(block);
                    return;
                }
            }
        }
        // Default to grid up/forward
        ShipUp = Base6Directions.Direction.Up;
        ShipForward = Base6Directions.Direction.Forward;
    }

    // Use orientation of a block of the given type
    public void SetShipReference<T>(IEnumerable<IMyTerminalBlock> blocks,
                                    Func<T, bool> condition = null)
        where T : IMyCubeBlock
    {
        var references = ZACommons.GetBlocksOfType<T>(blocks, condition);
        if (references.Count > 0)
        {
            SetShipReference(references[0]);
        }
        else
        {
            // Default to grid up/forward
            ShipUp = Base6Directions.Direction.Up;
            ShipForward = Base6Directions.Direction.Forward;
        }
    }
}
