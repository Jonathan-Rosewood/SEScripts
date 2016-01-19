// Begin configuration options

// Name of the group that should encompass everything relevant, which so far is:
//   Antennas, thrusters, gyros, beacons, batteries, spotlights and connectors (for MB Docking)
//   Flight seats, cockpits, remote controls, etc. (for Flighty Safety Dampeners)
//   Plus the special timer blocks, if any ("Safe Mode", "Low Battery")
// In can include more than this, or even simply include the whole ship.
// If this group doesn't exist, the script will look at all blocks that
// are on the same grid as this script's programmable block.
const string SHIP_NAME = "MyLander";

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check
