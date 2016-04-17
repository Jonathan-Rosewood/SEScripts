//@ commons eventdriver
public class DamageControl
{
    private const double RunDelay = 3.0;
    private const string ModeKey = "DamageConrol_Mode";

    private const int IDLE = 0;
    private const int ACTIVE = 1;
    private const int AUTO = 2;

    private int Mode = IDLE;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        Mode = IDLE;
        var modeString = commons.GetValue(ModeKey);
        if (modeString != null)
        {
            Mode = int.Parse(modeString);
            switch (Mode)
            {
                case IDLE:
                    break;
                case ACTIVE:
                    Start(commons, eventDriver, false);
                    break;
                case AUTO:
                    Start(commons, eventDriver, true);
                    break;
            }
        }
    }

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
                ResetMode(commons);
                break;
            case "show":
                Show(commons);
                ResetMode(commons);
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
        Show(commons);
        if (Mode == IDLE) eventDriver.Schedule(RunDelay, Run);
        Mode = auto ? AUTO : ACTIVE;
        SaveMode(commons);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode == IDLE) return;
        var damaged = Show(commons) > 0;
        if (Mode == ACTIVE || damaged)
        {
            eventDriver.Schedule(RunDelay, Run);
        }
        else
        {
            ResetMode(commons);
        }
    }

    public void Display(ZACommons commons)
    {
        if (Mode != IDLE)
        {
            commons.Echo("Damage Control: Active");
        }
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

    private void ResetMode(ZACommons commons)
    {
        Mode = IDLE;
        SaveMode(commons);
    }

    private void SaveMode(ZACommons commons)
    {
        commons.SetValue(ModeKey, Mode.ToString());
    }
}
