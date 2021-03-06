# utility #

The bulk of my reusable modules, usually dealing with a specific system or feature.

## Descriptions ##

 * batterymonitor &mdash; Monitors onboard batteries and starts a timer and/or calls some code if the average charge level falls below a certain percent.
 
 * cruisecontrol &mdash; While keeping dampeners engaged, it moves your ship at a specified speed in any of the 6 directions. Still allows for course corrections (unlike simply switching dampeners off) and helps to conserve energy/hydrogen.
 
 * dockinghandler &mdash; The `DockingHandler` interface. Just the interface. Needed by any module that implements this interface.
 
 * dockingmanager &mdash; Adds an "undock" and "dock" command. Smartly powers on & powers off things like thrusters, gyros, antennas, etc. when docked or undocked. Also provides hooks for other modules.
 
 * redundancy &mdash; Simple redundancy manager, ensures that a certain number of blocks in a block group are always enabled. Most useful for backup antennas and maybe backup gravity generators.
 
 * reversethrust &mdash; Assumes ship is not accelerating and dampeners are on. Disables all thrusters, measures current velocity, orients the specified facing toward the current velocity vector, then re-enables thrusters (which should bring the ship to a halt).

 * rotorrangefinder &mdash; Rotor-based rangefinder (parallax rangefinder)
 
 * rotorstepper &mdash; Uses a PID controller to allow for the precise stepping of rotors.

 * safemodehandler &mdash; The `SafeModeHandler` interface, used by the safemode module.

 * safemode &mdash; Re-engages dampeners if the ship is ever uncontrolled (will also trigger a timer block and/or call some code in that situation). Also starts a timer block if the ship is ever unattended for some amount of time.
 
 * smartundock &mdash; Smartly undocks a ship by thrusting it away from its connector. Memorizes the point some distance (e.g. 50 meters) from its mothership's connector. Provides a return-to-base command that will fly it (dumbly) back to that undock point.
 
   Also, the recent autopilot API changes to the remote control block kind of makes the custom autopilot moot. Though I did experiment with the RC-based autopilot, I opted to stick with the custom autopilot since most of my ships are drones (you cannot engage autopilot while controlling the drone).

 * solargyrocontroller &mdash; Module for keeping your solar panels always pointed at the sun by rotating the whole ship.
 
 * solarrotorcontroller &mdash; Module for keeping solar panels mounted on a single rotor pointed at the sun.
 
 * turretdetector &mdash; Proof-of-concept for detecting the presence of things that turrets can aim & fire at, e.g. meteors.
 
 * yawpitchauto &mdash; An autopilot that gets you from point A to point B. Points you toward point B and thrusts you forward at a defined speed.

