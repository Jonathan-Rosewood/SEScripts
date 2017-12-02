const string SHIP_GROUP = "MyShip";

// Custom Data: referenceGroup "My Remote Group"
const string VTVLHELPER_REMOTE_GROUP = "*MyShip Remote*";

const string RANGEFINDER_REFERENCE_GROUP = "Reference";
const string RANGEFINDER_TARGET_GROUP = "CM Target";
const string RANGEFINDER_TARGET_FORMAT = "GPS:Ranged Point:{0}:{1}:{2}:";

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check

const bool CONTROL_CHECK_ENABLED = true; // Typically true for drones or cockpit-based ships

// SmartUndock (ship-dependent)
const double SMART_UNDOCK_RTB_SPEED = 25.0; // In meters per second
const double AUTOPILOT_MIN_SPEED = 1.0; // In meters per second
const double AUTOPILOT_TTT_BUFFER = 5.0; // Time-to-target buffer, in seconds
const double AUTOPILOT_DISENGAGE_DISTANCE = 5.0; // In meters
const double AUTOPILOT_THRUST_DEAD_ZONE = 0.02; // Fraction of target speed
