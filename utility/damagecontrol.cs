//@ commons eventdriver
public class DamageControl
{
    private const double RunDelay = 3.0;

    private bool Active = false;

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();

        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length != 2 || parts[0] != "damecon") return;
        var command = parts[1];

        switch (command)
        {
            case "reset":
            case "stop":
                commons.AllBlocks.ForEach(block => {
                        if (block.GetProperty("ShowOnHUD") != null) block.SetValue<bool>("ShowOnHUD", false);
                    });
                Active = false;
                break;
            case "show":
                Show(commons);
                break;
            case "start":
                Show(commons);
                if (!Active)
                {
                    Active = true;
                    eventDriver.Schedule(RunDelay, Run);
                }
                break;
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!Active) return;
        //commons.Echo("Damage Control: Active");
        Active = Show(commons) > 0;
        eventDriver.Schedule(RunDelay, Run);
    }

    private uint Show(ZACommons commons)
    {
        uint count = 0;
        commons.AllBlocks.ForEach(block => {
                if (block.GetProperty("ShowOnHUD") != null)
                {
                    var cubeGrid = block.CubeGrid;
                    var damaged = !cubeGrid.GetCubeBlock(block.Position).IsFullIntegrity;
                    block.SetValue<bool>("ShowOnHUD", damaged);

                    if (damaged) count++;
                }
            });
        return count;
    }
}
