const string SHIP_NAME = "Simon";

// Should be a group with a single remote or some other ship controller block
const string MINER_REFERENCE_GROUP = "*Simon RC*";

// Should be a group with a single timer block. Block should run programmable
// block and do nothing else (not even start itself)
const string MINER_CLOCK_GROUP = "Simon Clock";

const double TARGET_MINING_SPEED = 0.5; // In meters per second

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check

// SmartUndock (ship-dependent)
const double SMART_UNDOCK_RTB_SPEED = 10.0; // In meters per second
const double AUTOPILOT_MIN_SPEED = 1.0; // In meters per second
const double AUTOPILOT_TTT_BUFFER = 10.0; // Time-to-target buffer, in seconds
const double AUTOPILOT_DISENGAGE_DISTANCE = 10.0; // In meters
