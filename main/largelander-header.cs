const string SHIP_GROUP = "MyLander";

// Most of the following options can be overridden by adding an appropriate
// line to the Custom Data of the prog block. (See comment that precedes each
// option.)

// Custom Data: referenceGroup "My Remote Group Name"
// (Be sure to include the quotes if your group name has spaces.)
const string VTVLHELPER_REMOTE_GROUP = "*MyLander Remote*";

const bool ABANDONMENT_ENABLED = false;

const bool CONTROL_CHECK_ENABLED = false;

// "oxygenManager no"
const bool OXYGEN_MANAGER_ENABLE = true;
// "airVentManager no"
const bool AIR_VENT_MANAGER_ENABLE = true;
// If other ships can dock to the lander
// "landerCarrier no"
const bool LANDER_CARRIER_ENABLE = true;
