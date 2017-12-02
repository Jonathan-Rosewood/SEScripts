const string SHIP_GROUP = "MyMiner";

// Custom Data: referenceGroup "My Remote Group"
// (Be sure to include the quotes if your group name has spaces.)
const string MINER_REFERENCE_GROUP = "*Simon RC*";

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check

const bool CONTROL_CHECK_ENABLED = true; // Typically true for drones or cockpit-based ships

// SmartUndock (ship-dependent)
const double SMART_UNDOCK_RTB_SPEED = 10.0; // In meters per second
const double AUTOPILOT_MIN_SPEED = 1.0; // In meters per second
const double AUTOPILOT_TTT_BUFFER = 10.0; // Time-to-target buffer, in seconds
const double AUTOPILOT_DISENGAGE_DISTANCE = 10.0; // In meters
const double AUTOPILOT_THRUST_DEAD_ZONE = 0.02; // Fraction of target speed
