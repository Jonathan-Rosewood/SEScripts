/*
 * ZerothAngel's Drone Controller script.
 *
 * Incorporates features from two other scripts along with a few new ones:
 *
 *     noszbytouj's MB Docking v0.2
 *     https://steamcommunity.com/sharedfiles/filedetails/?id=370171434
 *
 *     DanP's Flighty Safety Dampeners
 *     https://steamcommunity.com/sharedfiles/filedetails/?id=439033907
 *
 * Additional features:
 * - "Safe Mode" timer block started on safety dampeners engaging
 * - "Safe Mode" timer block started if drone has been abandoned: no one
 *   has used remote control or sat in a cockpit etc. after an hour (configurable)
 * - Battery check which starts the "Low Battery" timer block when charge
 *   falls below a threshold
 * - Rotor controller for maximum solar power (also released as my Solar Max Power script)
 */

// Begin configuration options

// Name of the group that should encompass everything relevant, which so far is:
//   Antennas, thrusters, gyros, beacons, batteries, spotlights and connectors (for MB Docking)
//   Flight seats, cockpits, remote controls, etc. (for Flighty Safety Dampeners)
//   Plus the special timer blocks, if any ("Safe Mode", "Low Battery")
// In can include more than this, or even simply include the whole ship.
// If this group doesn't exist, the script will look at all blocks that
// are on the same grid as this script's programmable block.
const string SHIP_NAME = "MyDrone";

const bool ABANDONMENT_ENABLED = true; // Set to false to disable abandonment check
const bool MAX_POWER_ENABLED = false; // Set to true to enable rotor code

// SmartUndock (ship-dependent)
const double SMART_UNDOCK_RTB_SPEED = 25.0; // In meters per second
const double SMART_UNDOCK_TTT_BUFFER = 2.5; // Time-to-target buffer, in seconds
const double SMART_UNDOCK_DISENGAGE_DISTANCE = 2.5; // In meters
