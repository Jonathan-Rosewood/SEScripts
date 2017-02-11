// CruiseControl
const double CRUISE_CONTROL_DEAD_ZONE = 0.02; // i.e. 2%

// Max gyro error when doing reverse thrust
// Increase (i.e. double or multiply by 5 or 10) if your ship
// takes too long to re-engage thrusters.
// The default (0.00004) is about .25 degrees for yaw & pitch.
const double REVERSE_THRUST_MAX_GYRO_ERROR = 0.00004;
