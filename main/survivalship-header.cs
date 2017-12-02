// Begin configuration

// Module enable/disable

// You can either edit the following to enable/disable modules or add the
// appropriate line to the prog block's Custom Data (see comment preceding
// each option). The Custom Data method is preferred since it makes upgrading
// the script easier.

// "autoCloseDoors no"
const bool AUTO_CLOSE_DOORS_ENABLE = true;
// "simpleAirlock no"
const bool SIMPLE_AIRLOCK_ENABLE = true;
// "complexAirlock no"
const bool COMPLEX_AIRLOCK_ENABLE = true;
// "oxygenManager no"
const bool OXYGEN_MANAGER_ENABLE = true;
// Air vent manager no longer compatible with complex airlocks as of 1.185.
// Only enable one or the other or neither.
// "airVentManager yes"
const bool AIR_VENT_MANAGER_ENABLE = false;
// "refineryManager no"
const bool REFINERY_MANAGER_ENABLE = true;
// "productionManager yes"
const bool PRODUCTION_MANAGER_ENABLE = false;
// "redundancyManager no"
const bool REDUNDANCY_MANAGER_ENABLE = true;
// "dockingAction no"
const bool DOCKING_ACTION_ENABLE = true;
// "damageControl no"
const bool DAMAGE_CONTROL_ENABLE = true;
// "reactorManager no"
const bool REACTOR_MANAGER_ENABLE = true;

// Options
const bool ABANDONMENT_ENABLED = false;
const bool CONTROL_CHECK_ENABLED = false;
