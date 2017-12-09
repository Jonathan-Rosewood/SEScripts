//@ commons eventdriver
public class DamageControl
{
    private const double RunDelay = 3.0;
    private const string ModeKey = "DamageConrol_Mode";

    enum Modes : int { Idle=0, Active=1, Auto=2 };

    private Modes Mode = Modes.Idle;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        Mode = Modes.Idle;
        var modeString = commons.GetValue(ModeKey);
        if (modeString != null)
        {
            var newMode = int.Parse(modeString);
            switch ((Modes)newMode)
            {
                case Modes.Idle:
                    break;
                case Modes.Active:
                    Start(commons, eventDriver, false);
                    break;
                case Modes.Auto:
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
        if (Mode == Modes.Idle) eventDriver.Schedule(RunDelay, Run);
        Mode = auto ? Modes.Auto : Modes.Active;
        SaveMode(commons);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode == Modes.Idle) return;
        var damaged = Show(commons) > 0;
        if (Mode == Modes.Active || damaged)
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
        if (Mode != Modes.Idle)
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
        Mode = Modes.Idle;
        SaveMode(commons);
    }

    private void SaveMode(ZACommons commons)
    {
        commons.SetValue(ModeKey, ((int)Mode).ToString());
    }
}
