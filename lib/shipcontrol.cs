//@ commons shiporientation gyrocontrol thrustcontrol
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

    public MyBlockOrientation ShipBlockOrientation
    {
        get { return shipOrientation.BlockOrientation; }
    }

    public ShipControlCommons(MyGridProgram program, UpdateType updateType,
                              ShipOrientation shipOrientation,
                              string shipGroup = null,
                              ZAStorage storage = null)
        : base(program, updateType, shipGroup: shipGroup, storage: storage)
    {
        this.shipOrientation = shipOrientation;
    }

    // GyroControl

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

    // ThrustControl

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

    // Utility

    public void Reset(bool gyroOverride = false,
                      bool? thrusterEnable = true,
                      Func<IMyThrust, bool> thrusterCondition = null)
    {
        GyroControl.Reset();
        GyroControl.EnableOverride(gyroOverride);
        ThrustControl.Reset(thrusterCondition);
        if (thrusterEnable != null) ThrustControl.Enable((bool)thrusterEnable, thrusterCondition);
    }

    // Reference vectors (i.e. orientation in world coordinates)

    public Vector3D ReferencePoint
    {
        get
        {
            if (m_referencePoint == null)
            {
                // Use center of mass, otherwise fall back to programmable block position
                m_referencePoint = ShipController != null ? ShipController.CenterOfMass : Me.GetPosition();
            }
            return (Vector3D)m_referencePoint;
        }
    }
    private Vector3D? m_referencePoint = null;

    public Vector3D ReferenceUp
    {
        get
        {
            if (m_referenceUp == null)
            {
                m_referenceUp = GetReferenceVector(shipOrientation.ShipUp);
            }
            return (Vector3D)m_referenceUp;
        }
    }
    private Vector3D? m_referenceUp = null;

    public Vector3D ReferenceForward
    {
        get
        {
            if (m_referenceForward == null)
            {
                m_referenceForward = GetReferenceVector(shipOrientation.ShipForward);
            }
            return (Vector3D)m_referenceForward;
        }
    }
    private Vector3D? m_referenceForward = null;

    public Vector3D ReferenceLeft
    {
        get
        {
            if (m_referenceLeft == null)
            {
                m_referenceLeft = GetReferenceVector(Base6Directions.GetLeft(shipOrientation.ShipUp, shipOrientation.ShipForward));
            }
            return (Vector3D)m_referenceLeft;
        }
    }
    private Vector3D? m_referenceLeft = null;

    private Vector3D GetReferenceVector(Base6Directions.Direction direction)
    {
        var offset = Me.Position + Base6Directions.GetIntVector(direction);
        return Vector3D.Normalize(Me.CubeGrid.GridIntegerToWorld(offset) - Me.GetPosition());
    }

    // IMyShipController

    // Return the very first functional controller we find.
    // NB Not suitable for grid reference, since it can be in any orientation
    // Also chicken/egg
    public IMyShipController ShipController
    {
        get
        {
            if (m_shipController == null)
            {
                foreach (var block in Blocks)
                {
                    var controller = block as IMyShipController;
                    if (controller != null && controller.IsFunctional)
                    {
                        m_shipController = controller;
                        break;
                    }
                }
            }
            return m_shipController;
        }
    }
    private IMyShipController m_shipController = null;

    // Convenience
    public Vector3D? LinearVelocity
    {
        get
        {
            return ShipController != null ?
                ShipController.GetShipVelocities().LinearVelocity : (Vector3D?)null;
        }
    }

    public Vector3D? AngularVelocity
    {
        get
        {
            return ShipController != null ?
                ShipController.GetShipVelocities().AngularVelocity : (Vector3D?)null;
        }
    }
}
