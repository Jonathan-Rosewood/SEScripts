public class ThrustControl
{
    private readonly Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();

    private void AddThruster(Base6Directions.Direction direction, IMyThrust thruster)
    {
        var thrusterList = GetThrusters(direction); // collect must be null to modify original list
        thrusterList.Add(thruster);
    }

    public void Init(IEnumerable<IMyTerminalBlock> blocks,
                     Func<IMyThrust, bool> collect = null,
                     Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        MyBlockOrientation shipOrientation = new MyBlockOrientation(shipForward, shipUp);

        thrusters.Clear();
        foreach (var block in blocks)
        {
            var thruster = block as IMyThrust;
            if (thruster != null && thruster.IsFunctional &&
                (collect == null || collect(thruster)))
            {
                var facing = thruster.Orientation.TransformDirection(Base6Directions.Direction.Forward); // Exhaust goes this way
                var thrustDirection = Base6Directions.GetFlippedDirection(facing);
                var shipDirection = shipOrientation.TransformDirectionInverse(thrustDirection);
                AddThruster(shipDirection, thruster);
            }
        }
    }

    public List<IMyThrust> GetThrusters(Base6Directions.Direction direction,
                                        Func<IMyThrust, bool> collect = null,
                                        bool disable = false)
    {
        List<IMyThrust> thrusterList;
        if (!thrusters.TryGetValue(direction, out thrusterList))
        {
            thrusterList = new List<IMyThrust>();
            thrusters.Add(direction, thrusterList);
        }
        if (collect == null)
        {
            return thrusterList;
        }
        else
        {
            var result = new List<IMyThrust>();
            foreach (var thruster in thrusterList)
            {
                if (collect(thruster))
                {
                    result.Add(thruster);
                }
                else if (disable)
                {
                    thruster.SetValue<bool>("OnOff", false);
                }
            }
            return result;
        }
    }

    public void SetOverride(Base6Directions.Direction direction, bool enable = true,
                            Func<IMyThrust, bool> collect = null)
    {
        var thrusterList = GetThrusters(direction, collect, true);
        thrusterList.ForEach(thruster =>
                             thruster.SetValue<float>("Override", enable ?
                                                      thruster.GetMaximum<float>("Override") :
                                                      0.0f));
    }

    public void SetOverride(Base6Directions.Direction direction, double percent,
                            Func<IMyThrust, bool> collect = null)
    {
        percent = Math.Max(percent, 0.0);
        percent = Math.Min(percent, 1.0);
        var thrusterList = GetThrusters(direction, collect, true);
        thrusterList.ForEach(thruster =>
                             thruster.SetValue<float>("Override",
                                                      (float)(thruster.GetMaximum<float>("Override") * percent)));
    }

    public void Enable(Base6Directions.Direction direction, bool enable,
                       Func<IMyThrust, bool> collect = null)
    {
        var thrusterList = GetThrusters(direction, collect, true);
        thrusterList.ForEach(thruster => thruster.SetValue<bool>("OnOff", enable));
    }

    public void Enable(bool enable,
                       Func<IMyThrust, bool> collect = null)
    {
        foreach (var thrusterList in thrusters.Values)
        {
            thrusterList.ForEach(thruster =>
                    {
                        if (collect == null || collect(thruster)) thruster.SetValue<bool>("OnOff", enable);
                    });
        }
    }

    public void Reset(Func<IMyThrust, bool> collect = null)
    {
        foreach (var thrusterList in thrusters.Values)
        {
            thrusterList.ForEach(thruster =>
                    {
                        if (collect == null || collect(thruster)) thruster.SetValue<float>("Override", 0.0f);
                    });
        }
    }
}
