# utility #

The bulk of my reusable modules, usually dealing with a specific system or feature.

## Descriptions ##

 * batterymanager &mdash; Battery management for solar-powered ships. Currently broken due to `IMyPowerProducer` removal. Also might no longer be needed since you uncheck *Recharge* and *Discharge* on the battery and it should behave in a similar fashion.
 
 * batterymonitor &mdash; Monitors onboard batteries and starts a timer and/or calls some code if the average charge level falls below a certain percent.
 
 * cruisecontrol &mdash; While keeping dampeners engaged, it moves your ship at a specified speed in any of the 6 directions. Still allows for course corrections (unlike simply switching dampeners off) and helps to conserve energy/hydrogen.
 
 * dockinghandler &mdash; The `DockingHandler` interface. Just the interface. Needed by any module that implements this interface.
 
 * dockingmanager &mdash; Adds an "undock" and "dock" command. Smartly powers on & powers off things like thrusters, gyros, antennas, etc. when docked or undocked. Also provides hooks for other modules.
 
 * powermanager &mdash; Now-defunct management of batteries as a true backup source. Would probably no longer be needed if the power priority put reactors above batteries (so that you don't needlessly use your batteries and thus expend 20% more power always charging them).
 
 * redundancy &mdash; Simple redundancy manager, ensures that a certain number of blocks in a block group are always enabled. Most useful for backup antennas and maybe backup gravity generators.
 
 * reversethrust &mdash; Assumes ship is not accelerating and dampeners are on. Disables all thrusters, measures current velocity, orients the specified facing toward the current velocity vector, then re-enables thrusters (which should bring the ship to a halt).

 * rotorrangefinder &mdash; Rotor-based rangefinder (parallax rangefinder)
 
 * rotorstepper &mdash; Uses a PID controller to allow for the precise stepping of rotors.

 * safemodehandler &mdash; The `SafeModeHandler` interface, used by the safemode module.

 * safemode &mdash; Re-engages dampeners if the ship is ever uncontrolled (will also trigger a timer block and/or call some code in that situation). Also starts a timer block if the ship is ever unattended for some amount of time.
 
 * smartundock &mdash; Smartly undocks a ship by thrusting it away from its connector. Memorizes the point some distance (e.g. 50 meters) from its mothership's connector. Provides a return-to-base command that will fly it (dumbly) back to that undock point.
 
   Also, the recent autopilot API changes to the remote control block kind of makes the custom autopilot moot. Though I did experiment with the RC-based autopilot, I opted to stick with the custom autopilot since most of my ships are drones (you cannot engage autopilot while controlling the drone).

 * solargyrocontroller &mdash; Now-defunct module for keeping your solar panels always pointed at the sun by rotating the whole ship. There's no reliable way of reading a solar panel's maximum potential power output anymore. (Sorry, I don't believe in parsing DetailInfo.)
 
 * solarrotorcontroller &mdash; Now-defunct module for keeping solar panels mounted on a rotor pointed at the sun.
 
 * translateauto &mdash; Simple autopilot that gets you from point A to point B by simply translating (strafing). Looks dumb, but it works and is very simple. Not really reliable or safe anymore due to dampener changes though.
 
 * turretdetector &mdash; Proof-of-concept for detecting the presence of things that turrets can aim & fire at, e.g. meteors.
 
 * yawpitchauto &mdash; Another autopilot that gets you from point A to point B. Points you toward point B and thrusts you forward at a defined speed.

