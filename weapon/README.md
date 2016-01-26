# weapon #

Weapon-related modules, mainly to my early guided missile work. Mostly defunct now due to multiple reasons:

 * The addition of cargo mass made the explosive/shrapnel type missiles useless.
 * Thruster/power/mass changes made it harder to achieve the necessary acceleration to stay maneuverable.
 * Block durability changes made even "armor piercing" missiles useless (i.e. they would often just harmlessly bounce off the targets even at 100+ m/s).

Note that the last two may no longer be true today. But my missiles no longer work (as effectively as they did), and as these changes happened, I lost more and more interest in weapon design in Space Engineers.

## Descriptions ##

 * missileguidance &mdash; Guidance script that would spiral the missile toward a given point (acquired usually via rangefinding). In its heydey, it was able to bring an *explosive* missile to its target even with 4-5 turrets firing.

 * missilelaunch &mdash; State machine for launching missiles ("state machine" being a fancy word for "chain of timer blocks simulator").
 
 * missilepayload &mdash; Given a cargo container with a single stack of items in its first slot, it will split the stack into multiple slots. In the olden days, this made the shrapnel-type missiles more deadly.
 
 * randomdecoy &mdash; Some random dude on Reddit claimed that strobing decoys would lock up turrets and I believed him. It's all B.S. His demo worked because his guns and decoys were *on the same grid*.
