//@ commons
public class SolarRotorController
{
    private const double RunDelay = 1.0;
    private const string ActiveKey = "SolarRotorController_Active";

    public struct SolarPanelDetails
    {
        public float MaxPowerOutput;
        public float DefinedPowerOutput;

        public SolarPanelDetails(ZACommons.BlockGroup group)
        {
            MaxPowerOutput = 0.0f;
            DefinedPowerOutput = 0.0f;

            for (var e = group.Blocks.GetEnumerator(); e.MoveNext();)
            {
                var panel = e.Current as IMySolarPanel;

                if (panel != null && panel.IsFunctional && panel.IsWorking)
                {
                    MaxPowerOutput += panel.MaxOutput;
                    DefinedPowerOutput += panel.CubeGrid.GridSize == 2.5f ? SOLAR_PANEL_MAX_POWER_LARGE : SOLAR_PANEL_MAX_POWER_SMALL;
                }
            }
        }
    }

    private readonly Dictionary<string, float> MaxPowers = new Dictionary<string, float>();

    private bool Active = false;
    private float TotalPower;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        Active = true;
        SaveActive(commons);
        MaxPowers.Clear();
        TotalPower = 0.0f;
        eventDriver.Schedule(0.0, Run);
    }

    public void ConditionalInit(ZACommons commons, EventDriver eventDriver,
                                bool defaultActive = false)
    {
        var activeValue = commons.GetValue(ActiveKey);
        if (activeValue != null)
        {
            bool active;
            if (Boolean.TryParse(activeValue, out active))
            {
                if (active) Init(commons, eventDriver);
                return;
            }
        }
        if (defaultActive) Init(commons, eventDriver);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!Active) return;

        TotalPower = 0.0f;
        var solarGroups = commons.GetBlockGroupsWithPrefix(MAX_POWER_GROUP_PREFIX);
        for (var e = solarGroups.GetEnumerator(); e.MoveNext();)
        {
            var group = e.Current;

            var rotor = GetRotor(group);
            if (rotor == null)
            {
                commons.Echo(string.Format("Group {0} ignored; needs exactly 1 rotor", group.Name));
                continue;
            }
            else if (rotor.CubeGrid != commons.Me.CubeGrid)
            {
                // Skip if rotor is on a different grid than this programmable block
                continue;
            }

            var solarPanelDetails = new SolarPanelDetails(group);
            var currentMaxPower = solarPanelDetails.MaxPowerOutput;

            float maxPower;
            if (!MaxPowers.TryGetValue(group.Name, out maxPower)) maxPower = -100.0f;

            var minError = solarPanelDetails.DefinedPowerOutput * SOLAR_ROTOR_MIN_ERROR;
            var delta = currentMaxPower - maxPower;
            MaxPowers[group.Name] = currentMaxPower;

            if (delta > minError || currentMaxPower < minError /* failsafe */)
            {
                // Keep going
                rotor.SetValue<bool>("OnOff", true);
            }
            else if (delta < -minError)
            {
                // Back up
                rotor.SetValue<bool>("OnOff", true);
                rotor.ApplyAction("Reverse");
            }
            else
            {
                // Hold still for a moment
                rotor.SetValue<bool>("OnOff", false);
            }

            TotalPower += currentMaxPower;
        }

        eventDriver.Schedule(RunDelay, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        // Handle commands
        argument = argument.Trim().ToLower();
        if (argument == "pause")
        {
            Active = false;
            SaveActive(commons);
            var solarGroups = commons.GetBlockGroupsWithPrefix(MAX_POWER_GROUP_PREFIX);
            solarGroups.ForEach(group => {
                    var rotor = GetRotor(group);
                    if (rotor != null) rotor.SetValue<bool>("OnOff", false);
                });
        }
        else if (argument == "resume")
        {
            if (!Active) Init(commons, eventDriver);
        }
    }

    public void Display(ZACommons commons)
    {
        if (Active)
        {
            commons.Echo(string.Format("Solar Max Power: {0}", ZACommons.FormatPower(TotalPower)));
        }
        else
        {
            commons.Echo("Solar Max Power: Paused");
        }
    }

    private IMyMotorStator GetRotor(ZACommons.BlockGroup group)
    {
        var rotors = ZACommons.GetBlocksOfType<IMyMotorStator>(group.Blocks);
        return rotors.Count == 1 ? (IMyMotorStator)rotors[0] : null;
    }

    private void SaveActive(ZACommons commons)
    {
        commons.SetValue(ActiveKey, Active.ToString());
    }
}
