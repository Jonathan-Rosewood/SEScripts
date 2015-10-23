public class DamageControl
{
    private const double RunDelay = 3.0;

    private const int STATE_INACTIVE = 0;
    private const int STATE_ACTIVE = 1;
    private const int STATE_INACTIVATING = -1;

    private int State = STATE_INACTIVE;

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();

        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length != 2 || parts[0] != "damecon") return;
        argument = parts[1];

        switch (argument)
        {
            case "reset":
            case "stop":
                {
                    commons.AllBlocks.ForEach(block => {
                            block.SetValue<bool>("ShowOnHUD", false);
                        });
                    if (State != STATE_INACTIVE) State = STATE_INACTIVATING;
                    break;
                }
            case "show":
                {
                    Show(commons);
                    break;
                }
            case "start":
                {
                    Show(commons);
                    if (State == STATE_INACTIVE) eventDriver.Schedule(RunDelay, Run);
                    State = STATE_ACTIVE;
                    break;
                }
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (State != STATE_ACTIVE)
        {
            State = STATE_INACTIVE;
            return;
        }

        Show(commons);
        eventDriver.Schedule(RunDelay, Run);
    }

    private void Show(ZACommons commons)
    {
        commons.AllBlocks.ForEach(block => {
                var cubeGrid = block.CubeGrid;
                var damaged = !cubeGrid.GetCubeBlock(block.Position).IsFullIntegrity;
                block.SetValue<bool>("ShowOnHUD", damaged);
            });
    }
}
