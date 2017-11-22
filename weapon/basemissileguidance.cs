//@ commons eventdriver
public abstract class BaseMissileGuidance
{
    protected Vector3D Target, TargetVelocity;
    protected TimeSpan LastTargetUpdate;

    // An update from e.g. an onboard seeker
    public virtual void UpdateTarget(EventDriver eventDriver, Vector3D target, Vector3D velocity)
    {
        Target = target;
        TargetVelocity = velocity;
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    // A remote update (antenna, another PB, etc.)
    public virtual void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length != 7) return;
        if (parts[0] != "tupdate") return;
        var target = new Vector3D();
        for (int i = 1; i < 4; i++)
        {
            target.SetDim(i-1, double.Parse(parts[i]));
        }
        var targetVelocity = new Vector3D();
        for (int i = 4; i < 7; i++)
        {
            targetVelocity.SetDim(i-4, double.Parse(parts[i]));
        }
        UpdateTarget(eventDriver, target, targetVelocity);
    }
}
