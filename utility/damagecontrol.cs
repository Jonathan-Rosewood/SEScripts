//@ commons eventdriver
public class DamageControl
{
    private const double RunDelay = 3.0;

    private bool Active = false;
    private bool Auto = false;

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
                Active = false;
                break;
            case "start":
                Start(commons, eventDriver, false);
                break;
            case "auto":
                Start(commons, eventDriver, true);
                break;
        }
    }

    private void Start(ZACommons commons, EventDriver eventDriver, bool auto)
    {
        Auto = auto;
        Show(commons);
        if (!Active)
        {
            Active = true;
            eventDriver.Schedule(RunDelay, Run);
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!Active) return;
        //commons.Echo("Damage Control: Active");
        Active = Show(commons) > 0 || !Auto;
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
