//@ commons
public class SolarRotorController
{
    public struct SolarPanelDetails
    {
        public float MaxPowerOutput;
        public float DefinedPowerOutput;

        public SolarPanelDetails(IMyBlockGroup group)
        {
            MaxPowerOutput = 0.0f;
            DefinedPowerOutput = 0.0f;

            for (var e = group.Blocks.GetEnumerator(); e.MoveNext();)
            {
                var panel = e.Current as IMySolarPanel;

                if (panel != null && panel.IsFunctional && panel.IsWorking)
                {
                    MaxPowerOutput += panel.MaxPowerOutput;
                    DefinedPowerOutput += panel.DefinedPowerOutput;
                }
            }
        }
    }

    private readonly Dictionary<string, float> maxPowers = new Dictionary<string, float>();

    private IMyMotorStator GetRotor(IMyBlockGroup group)
    {
        var rotors = ZACommons.GetBlocksOfType<IMyMotorStator>(group.Blocks);
        return rotors.Count == 1 ? rotors[0] : null;
    }

    public void Run(ZACommons commons)
    {
        var solarGroups = commons.GetBlockGroupsWithPrefix(MAX_POWER_GROUP_PREFIX);
        if (solarGroups.Count == 0) return; // Nothing to do

        var totalPower = 0.0f;

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
            if (!maxPowers.TryGetValue(group.Name, out maxPower)) maxPower = -100.0f;

            var minError = solarPanelDetails.DefinedPowerOutput * 0.005f; // From experimentation
            var delta = currentMaxPower - maxPower;

            if (delta > minError || currentMaxPower < minError /* failsafe*/)
            {
                // Keep going
                if (!rotor.Enabled) rotor.GetActionWithName("OnOff_On").Apply(rotor);
                maxPowers[group.Name] = currentMaxPower;
            }
            else if (delta < -minError)
            {
                // Back up
                if (!rotor.Enabled) rotor.GetActionWithName("OnOff_On").Apply(rotor);
                rotor.GetActionWithName("Reverse").Apply(rotor);
                maxPowers[group.Name] = currentMaxPower;
            }
            else
            {
                // Hold still for a moment
                if (rotor.Enabled) rotor.GetActionWithName("OnOff_Off").Apply(rotor);
                // Note, we don't save current max power
            }

            totalPower += currentMaxPower;
        }

        commons.Echo(string.Format("Solar Max Power: {0}", ZACommons.FormatPower(totalPower)));
    }
}
