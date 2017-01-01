public class GyroControl
{
    public const int Yaw = 0;
    public const int Pitch = 1;
    public const int Roll = 2;

    public readonly string[] AxisNames = new string[] { "Yaw", "Pitch", "Roll" };

    public struct GyroAxisDetails
    {
        public int LocalAxis;
        public int Sign;

        public GyroAxisDetails(int localAxis, int sign)
        {
            LocalAxis = localAxis;
            Sign = sign;
        }
    }

    public struct GyroDetails
    {
        public IMyGyro Gyro;
        public GyroAxisDetails[] AxisDetails;

        public GyroDetails(IMyGyro gyro, Base6Directions.Direction shipUp,
                           Base6Directions.Direction shipForward)
        {
            Gyro = gyro;
            AxisDetails = new GyroAxisDetails[3];

            var shipLeft = Base6Directions.GetLeft(shipUp, shipForward);

            // Determine yaw axis
            SetAxisDetails(gyro, Yaw, shipUp);

            // Determine pitch axis
            SetAxisDetails(gyro, Pitch, shipLeft);

            // Determine roll axis
            SetAxisDetails(gyro, Roll, shipForward);
        }

        private void SetAxisDetails(IMyGyro gyro, int axis,
                                    Base6Directions.Direction axisDirection)
        {
            switch (gyro.Orientation.TransformDirectionInverse(axisDirection))
            {
                case Base6Directions.Direction.Up:
                    AxisDetails[axis] = new GyroAxisDetails(Yaw, -1);
                    break;
                case Base6Directions.Direction.Down:
                    AxisDetails[axis] = new GyroAxisDetails(Yaw, 1);
                    break;
                case Base6Directions.Direction.Left:
                    AxisDetails[axis] = new GyroAxisDetails(Pitch, -1);
                    break;
                case Base6Directions.Direction.Right:
                    AxisDetails[axis] = new GyroAxisDetails(Pitch, 1);
                    break;
                case Base6Directions.Direction.Forward:
                    AxisDetails[axis] = new GyroAxisDetails(Roll, 1);
                    break;
                case Base6Directions.Direction.Backward:
                    AxisDetails[axis] = new GyroAxisDetails(Roll, -1);
                    break;
            }

        }
    }

    private readonly List<GyroDetails> gyros = new List<GyroDetails>();

    public void Init(IEnumerable<IMyTerminalBlock> blocks,
                     Func<IMyGyro, bool> collect = null,
                     Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        gyros.Clear();
        foreach (var block in blocks)
        {
            var gyro = block as IMyGyro;
            if (gyro != null &&
                gyro.IsFunctional && gyro.IsWorking && gyro.Enabled &&
                (collect == null || collect(gyro)))
            {
                var details = new GyroDetails(gyro, shipUp, shipForward);
                gyros.Add(details);
            }
        }
    }

    public void EnableOverride(bool enable)
    {
        gyros.ForEach(gyro => gyro.Gyro.SetValue<bool>("Override", enable));
    }

    public void SetAxisVelocity(int axis, float velocity)
    {
        gyros.ForEach(gyro => gyro.Gyro.SetValue<float>(AxisNames[gyro.AxisDetails[axis].LocalAxis], gyro.AxisDetails[axis].Sign * velocity));
    }

    public void SetAxisVelocityRPM(int axis, float rpmVelocity)
    {
        SetAxisVelocity(axis, rpmVelocity * MathHelper.RPMToRadiansPerSecond);
    }

    public void SetAxisVelocityFraction(int axis, float velocity)
    {
        gyros.ForEach(gyro => {
                var axisName = AxisNames[gyro.AxisDetails[axis].LocalAxis];
                gyro.Gyro.SetValue<float>(axisName, gyro.Gyro.GetMaximum<float>(axisName) * gyro.AxisDetails[axis].Sign * velocity);
            });
    }

    public void Reset()
    {
        gyros.ForEach(gyro => {
                gyro.Gyro.SetValue<float>("Yaw", 0.0f);
                gyro.Gyro.SetValue<float>("Pitch", 0.0f);
                gyro.Gyro.SetValue<float>("Roll", 0.0f);
            });
    }
}
