// Do not hold a reference to an instance of this class between runs
public class ShipControlCommons : ZACommons
{
    private readonly ShipOrientation shipOrientation;

    public Base6Directions.Direction ShipUp
    {
        get { return shipOrientation.ShipUp; }
    }

    public Base6Directions.Direction ShipForward
    {
        get { return shipOrientation.ShipForward; }
    }

    public ShipControlCommons(MyGridProgram program,
                              ShipOrientation shipOrientation,
                              string shipGroup = null)
        : base(program, shipGroup: shipGroup)
    {
        this.shipOrientation = shipOrientation;
    }

    public GyroControl GyroControl
    {
        get
        {
            if (m_gyroControl == null)
            {
                m_gyroControl = new GyroControl();
                m_gyroControl.Init(Blocks,
                                   shipUp: shipOrientation.ShipUp,
                                   shipForward: shipOrientation.ShipForward);
            }
            return m_gyroControl;
        }
    }
    private GyroControl m_gyroControl = null;

    public ThrustControl ThrustControl
    {
        get
        {
            if (m_thrustControl == null)
            {
                m_thrustControl = new ThrustControl();
                m_thrustControl.Init(Blocks,
                                     shipUp: shipOrientation.ShipUp,
                                     shipForward: shipOrientation.ShipForward);
            }
            return m_thrustControl;
        }
    }
    private ThrustControl m_thrustControl = null;
}
