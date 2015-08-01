// Should be a group with a single remote or some other ship controller block
const string MINER_REFERENCE_GROUP = "*Simon RC*";

// Should be a group with a single timer block. Block should run programmable
// block and do nothing else (not even start itself)
const string MINER_CLOCK_GROUP = "Simon Clock";

const double TARGET_MINING_SPEED = 1.0; // In meters per second

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check
