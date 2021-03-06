# weapon #

Weapon-related modules, mainly to my early guided missile work. Mostly defunct now due to multiple reasons:

 * The addition of cargo mass made the explosive/shrapnel type missiles useless.
 * Thruster/power/mass changes made it harder to achieve the necessary acceleration to stay maneuverable.
 * Block durability changes made even kinetic missiles useless (i.e. they would often just harmlessly bounce off the targets even at 100+ m/s).

Note that the last two may no longer be true today. But my missiles no longer work (as effectively as they did), and as these changes happened, I lost more and more interest in weapon design in Space Engineers.

## Descriptions ##

 * guidancekill &mdash; Module that disables on-board timer blocks (effectively shutting down any scripts), resets gyros, and finally disables thrusters when any damage is detected on any prog blocks/timers.

 * losguidance &mdash; Guidance system that snapshots the orientation of some reference block (e.g. a remote with a camera in front) and then guides the missile on a simple straight-line trajectory following the reference block's line-of-sight. The missile can be fired in any orientation (forwards, sideways, backwards, whatever) and it will fly (conservatively), line up, and then thrust at maximum toward the target. Can also guide missiles in real time, but that would require holding a reference to the reference block (which is cheesy)...

 * missileguidance &mdash; Guidance script that would spiral the missile toward a given point (acquired usually via rangefinding). In its heydey, it was able to bring an *explosive* missile to its target even with 4-5 turrets firing.

 * missilelaunch &mdash; State machine for launching missiles ("state machine" being a fancy word for "chain of timer blocks simulator").
