//@ commons eventdriver
public abstract class BaseMissileGuidance
{
    // Primary
    protected long TargetID;
    protected Vector3D TargetPosition, TargetVelocity, TargetOffset;
    protected MatrixD TargetOrientation;
    protected TimeSpan LastTargetUpdate;

    // Derived
    protected Vector3D TargetAimPoint;

    protected virtual void TargetUpdated(EventDriver eventDriver)
    {
        // Re-derive any derived properties of the target
        TargetAimPoint = TargetPosition + Vector3D.Transform(TargetOffset, TargetOrientation);
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    // An update from e.g. an onboard seeker
    public void UpdateTarget(EventDriver eventDriver, MyDetectedEntityInfo info, bool full = false)
    {
        TargetPosition = info.Position;
        TargetVelocity = new Vector3D(info.Velocity);
        TargetOrientation = info.Orientation;

        if (full)
        {
            TargetID = info.EntityId;
            var offset = (Vector3D)info.HitPosition - TargetPosition;
            var toLocal = MatrixD.Invert(info.Orientation);
            TargetOffset = Vector3D.Transform(offset, toLocal);
        }

        TargetUpdated(eventDriver);
    }

    // A remote update (antenna, another PB, etc.)
    public virtual void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length != 15) return;
        if (parts[0] != "tupdate") return;
        TargetID = long.Parse(parts[1]);
        TargetPosition = new Vector3D();
        for (int i = 2; i < 5; i++)
        {
            TargetPosition.SetDim(i-2, double.Parse(parts[i]));
        }
        TargetVelocity = new Vector3D();
        for (int i = 5; i < 8; i++)
        {
            TargetVelocity.SetDim(i-5, double.Parse(parts[i]));
        }
        var orientation = new QuaternionD();
        orientation.X = double.Parse(parts[8]);
        orientation.Y = double.Parse(parts[9]);
        orientation.Z = double.Parse(parts[10]);
        orientation.W = double.Parse(parts[11]);
        TargetOrientation = MatrixD.CreateFromQuaternion(orientation);
        // Having a different message would save us transmitting the offset...
        // But then messaging becomes stateful because we'd need the init +
        // update messages...
        TargetOffset = new Vector3D();
        for (int i = 12; i < 15; i++)
        {
            TargetOffset.SetDim(i-12, double.Parse(parts[i]));
        }
        TargetUpdated(eventDriver);
    }
}
