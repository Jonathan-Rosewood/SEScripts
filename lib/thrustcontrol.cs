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
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var thruster = e.Current as IMyThrust;
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
                                        Func<IMyThrust, bool> collect = null)
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
            for (var e = thrusterList.GetEnumerator(); e.MoveNext();)
            {
                var thruster = (IMyThrust)e.Current;
                if (collect(thruster)) result.Add(thruster);
            }
            return result;
        }
    }

    public void SetOverride(Base6Directions.Direction direction, bool enable = true,
                            Func<IMyThrust, bool> collect = null)
    {
        var thrusterList = GetThrusters(direction, collect);
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
        var thrusterList = GetThrusters(direction, collect);
        thrusterList.ForEach(thruster =>
                             thruster.SetValue<float>("Override",
                                                      (float)(thruster.GetMaximum<float>("Override") * percent)));
    }

    public void SetOverrideNewtons(Base6Directions.Direction direction, double force,
                                   Func<IMyThrust, bool> collect = null)
    {
        var thrusterList = GetThrusters(direction, collect);
        var maxForce = 0.0;
        thrusterList.ForEach(thruster =>
                             maxForce += thruster.GetMaximum<float>("Override"));
        // Constrain
        force = Math.Max(force, 0.0);
        force = Math.Min(force, maxForce);
        // Each thruster outputs its own share
        var fraction = force / maxForce;
        thrusterList.ForEach(thruster =>
                             thruster.SetValue<float>("Override",
                                                      (float)(fraction * thruster.GetMaximum<float>("Override"))));
    }

    public void Enable(Base6Directions.Direction direction, bool enable,
                       Func<IMyThrust, bool> collect = null)
    {
        var thrusterList = GetThrusters(direction, collect);
        thrusterList.ForEach(thruster => thruster.SetValue<bool>("OnOff", enable));
    }

    public void Enable(bool enable,
                       Func<IMyThrust, bool> collect = null)
    {
        for (var e = thrusters.Values.GetEnumerator(); e.MoveNext();)
        {
            var thrusterList = e.Current;
            thrusterList.ForEach(thruster =>
                    {
                        if (collect == null || collect(thruster)) thruster.SetValue<bool>("OnOff", enable);
                    });
        }
    }

    public void Reset(Func<IMyThrust, bool> collect = null)
    {
        for (var e = thrusters.Values.GetEnumerator(); e.MoveNext();)
        {
            var thrusterList = e.Current;
            thrusterList.ForEach(thruster =>
                    {
                        if (collect == null || collect(thruster)) thruster.SetValue<float>("Override", 0.0f);
                    });
        }
    }
}
