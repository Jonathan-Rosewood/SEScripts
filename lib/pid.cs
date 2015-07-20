public class PIDController
{
    public readonly double dt; // i.e. 1.0 / ticks per second

    public double Kp { get; set; }

    public double Ki
    {
        get { return m_Ki; }
        set { m_Ki = value; m_Kidt = m_Ki * dt; }
    }
    private double m_Ki, m_Kidt;

    public double Kd
    {
        get { return m_Kd; }
        set { m_Kd = value; m_Kddt = m_Kd / dt; }
    }
    private double m_Kd, m_Kddt;

    private double integral = 0.0;
    private double lastError = 0.0;

    public PIDController(double dt)
    {
        this.dt = dt;
    }

    public void Reset()
    {
        integral = 0.0;
        lastError = 0.0;
    }

    public double Compute(double error)
    {
        integral += error;
        var derivative = error - lastError;
        lastError = error;

        return ((Kp * error) +
                (m_Kidt * integral) +
                (m_Kddt * derivative));
    }
}
