//@ shipcontrol eventdriver
public class SpeedAction
{
    private const double RunDelay = 1.0;
    private const char ACTION_DELIMETER = ':';

    private double LastSpeed;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        LastSpeed = 0.0;

        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var controller = shipControl.ShipController;
        if (controller != null)
        {
            var speed = controller.GetShipSpeed();
            DoActions(commons, eventDriver, speed);
            LastSpeed = (double)speed;
        }

        eventDriver.Schedule(RunDelay, Run);
    }

    private void DoActions(ZACommons commons, EventDriver eventDriver,
                           double currentSpeed)
    {
        foreach (var group in commons.GetBlockGroupsWithPrefix(SPEED_ACTION_PREFIX))
        {
            // Break it up and parse each part
            var parts = group.Name.Split(new char[] { ACTION_DELIMETER }, 3);

            if (parts.Length < 2) continue; // Need at least speed
            double speed;
            if (!double.TryParse(parts[1], out speed)) continue; // And it needs to be parsable

            string action = "on";
            if (parts.Length == 3)
            {
                action = parts[2];
            }

            var rising = false;
            var falling = false;
            switch (action[0])
            {
                case '>':
                    rising = true;
                    action = action.Substring(1);
                    break;
                case '<':
                    falling = true;
                    action = action.Substring(1);
                    break;
            }

            var onFlag = "on".Equals(action, ZACommons.IGNORE_CASE);
            var offFlag = "off".Equals(action, ZACommons.IGNORE_CASE);

            if (onFlag || offFlag)
            {
                if (!rising && !falling)
                {
                    bool enable;
                    if (onFlag)
                    {
                        enable = currentSpeed >= speed;
                    }
                    else
                    {
                        enable = currentSpeed < speed;
                    }

                    group.Blocks.ForEach(block =>
                                         block.SetValue<bool>("OnOff", enable));
                }
                else if ((rising && LastSpeed < speed && currentSpeed >= speed) ||
                         (falling && LastSpeed >= speed && currentSpeed < speed))
                {
                    group.Blocks.ForEach(block =>
                                         block.SetValue<bool>("OnOff", onFlag));
                }
            }
        }
    }
}
