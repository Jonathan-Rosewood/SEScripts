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
            switch (gyro.Orientation.TransformDirectionInverse(shipUp))
            {
                case Base6Directions.Direction.Up:
                    AxisDetails[Yaw] = new GyroAxisDetails(Yaw, -1);
                    break;
                case Base6Directions.Direction.Down:
                    AxisDetails[Yaw] = new GyroAxisDetails(Yaw, 1);
                    break;
                case Base6Directions.Direction.Left:
                    AxisDetails[Yaw] = new GyroAxisDetails(Pitch, 1);
                    break;
                case Base6Directions.Direction.Right:
                    AxisDetails[Yaw] = new GyroAxisDetails(Pitch, -1);
                    break;
                case Base6Directions.Direction.Forward:
                    AxisDetails[Yaw] = new GyroAxisDetails(Roll, 1);
                    break;
                case Base6Directions.Direction.Backward:
                    AxisDetails[Yaw] = new GyroAxisDetails(Roll, -1);
                    break;
            }

            // Determine pitch axis
            switch (gyro.Orientation.TransformDirectionInverse(shipLeft))
            {
                case Base6Directions.Direction.Up:
                    AxisDetails[Pitch] = new GyroAxisDetails(Yaw, -1);
                    break;
                case Base6Directions.Direction.Down:
                    AxisDetails[Pitch] = new GyroAxisDetails(Yaw, 1);
                    break;
                case Base6Directions.Direction.Left:
                    AxisDetails[Pitch] = new GyroAxisDetails(Pitch, -1);
                    break;
                case Base6Directions.Direction.Right:
                    AxisDetails[Pitch] = new GyroAxisDetails(Pitch, 1);
                    break;
                case Base6Directions.Direction.Forward:
                    AxisDetails[Pitch] = new GyroAxisDetails(Roll, 1);
                    break;
                case Base6Directions.Direction.Backward:
                    AxisDetails[Pitch] = new GyroAxisDetails(Roll, -1);
                    break;
            }

            // Determine roll axis
            switch (gyro.Orientation.TransformDirectionInverse(shipForward))
            {
                case Base6Directions.Direction.Up:
                    AxisDetails[Roll] = new GyroAxisDetails(Yaw, -1);
                    break;
                case Base6Directions.Direction.Down:
                    AxisDetails[Roll] = new GyroAxisDetails(Yaw, 1);
                    break;
                case Base6Directions.Direction.Left:
                    AxisDetails[Roll] = new GyroAxisDetails(Pitch, -1);
                    break;
                case Base6Directions.Direction.Right:
                    AxisDetails[Roll] = new GyroAxisDetails(Pitch, 1);
                    break;
                case Base6Directions.Direction.Forward:
                    AxisDetails[Roll] = new GyroAxisDetails(Roll, 1);
                    break;
                case Base6Directions.Direction.Backward:
                    AxisDetails[Roll] = new GyroAxisDetails(Roll, -1);
                    break;
            }
        }
    }

    private readonly List<GyroDetails> gyros = new List<GyroDetails>();

    public void Init(MyGridProgram program,
                     List<IMyTerminalBlock> blocks = null,
                     Func<IMyGyro, bool> collect = null,
                     Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
    {
        if (blocks == null)
        {
            blocks = new List<IMyTerminalBlock>();
            program.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks,
                                                                block => block.CubeGrid == program.Me.CubeGrid);
        }

        gyros.Clear();
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var gyro = e.Current as IMyGyro;
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

    public void Reset()
    {
        gyros.ForEach(gyro => {
                gyro.Gyro.SetValue<float>("Yaw", 0.0f);
                gyro.Gyro.SetValue<float>("Pitch", 0.0f);
                gyro.Gyro.SetValue<float>("Roll", 0.0f);
            });
    }
}
