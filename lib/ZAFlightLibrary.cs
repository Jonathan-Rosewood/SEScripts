// ZAFlightLibrary v1.0.0
public static class ZAFlightLibrary
{
    // Why no enums, Keen?!
    public const int GyroAxisYaw = 0;
    public const int GyroAxisPitch = 1;
    public const int GyroAxisRoll = 2;

    public static bool GetAutoPilotState(IMyRemoteControl remote)
    {
        return remote.GetValue<bool>("AutoPilot");
    }

    public static void SetThrusterOverride(List<IMyTerminalBlock> thrusters, float force)
    {
        for (var e = thrusters.GetEnumerator(); e.MoveNext();)
        {
            var thruster = e.Current as IMyThrust;
            if (thruster != null) thruster.SetValue<float>("Override", force);
        }
    }

    public static void EnableGyroOverride(IMyGyro gyro, bool enable)
    {
        if ((gyro.GyroOverride && !enable) ||
            (!gyro.GyroOverride && enable))
        {
            gyro.GetActionWithName("Override").Apply(gyro);
        }
    }

    public static void EnableGyroOverride(List<IMyGyro> gyros, bool enable)
    {
        for (var e = gyros.GetEnumerator(); e.MoveNext();)
        {
            EnableGyroOverride(e.Current, enable);
        }
    }

    public static void SetAxisVelocity(IMyGyro gyro, int axis, float velocity)
    {
        switch (axis)
        {
            case GyroAxisYaw:
                gyro.SetValue<float>("Yaw", velocity);
                break;
            case GyroAxisPitch:
                gyro.SetValue<float>("Pitch", velocity);
                break;
            case GyroAxisRoll:
                gyro.SetValue<float>("Roll", velocity);
                break;
        }
    }

    public static void SetAxisVelocity(List<IMyGyro> gyros, int axis, float velocity)
    {
        for (var e = gyros.GetEnumerator(); e.MoveNext();)
        {
            SetAxisVelocity(e.Current, axis, velocity);
        }
    }

    public static float GetAxisVelocity(IMyGyro gyro, int axis)
    {
        switch (axis)
        {
            case GyroAxisYaw:
                return gyro.Yaw;
            case GyroAxisPitch:
                return gyro.Pitch;
            case GyroAxisRoll:
                return gyro.Roll;
        }
        return default(float);
    }

    public static void ReverseAxisVelocity(IMyGyro gyro, int axis)
    {
        float? velocity = null;

        switch (axis)
        {
            case GyroAxisYaw:
                velocity = -gyro.Yaw;
                break;
            case GyroAxisPitch:
                velocity = -gyro.Pitch;
                break;
            case GyroAxisRoll:
                velocity = -gyro.Roll;
                break;
        }

        if (velocity != null) SetAxisVelocity(gyro, axis, (float)velocity);
    }

    public static void ResetGyro(IMyGyro gyro)
    {
        SetAxisVelocity(gyro, GyroAxisYaw, 0.0f);
        SetAxisVelocity(gyro, GyroAxisPitch, 0.0f);
        SetAxisVelocity(gyro, GyroAxisRoll, 0.0f);
    }
}
