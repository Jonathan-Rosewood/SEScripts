const bool ABANDONMENT_ENABLED = false;

const bool CONTROL_CHECK_ENABLED = true;

// Module enable/disable
const bool AUTO_CLOSE_DOORS_ENABLE = true;
const bool SIMPLE_AIRLOCK_ENABLE = true;
const bool OXYGEN_MANAGER_ENABLE = true;

// Max gyro error when doing reverse thrust
// Increase (i.e. double or multiply by 5 or 10) if your ship
// takes too long to re-engage thrusters.
// The default (0.00004) is about .25 degrees for yaw & pitch.
const double REVERSE_THRUST_MAX_GYRO_ERROR = 0.00004;
