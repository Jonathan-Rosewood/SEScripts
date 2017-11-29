// FireControl
const string FC_MAIN_CAMERA_GROUP = "FireControlCamera";
const double FC_INITIAL_RAYCAST_RANGE = 10000.0; // In meters

const string FC_FIRE_GROUP = "Shell Prime";
// The following is the offset of the shell from the ship's CoM.
// Since the entire ship is being aimed, the shell would actually start
// a little ahead. It should also account for acceleration, i.e.
// the point at which the shell is going FC_SHELL_SPEED.
const double FC_SHELL_OFFSET = 21.0 + 32.77; // In meters
// The following is pretty much a fudge factor to account for any firing
// delay as well as acceleration delay.
const double FC_FIRE_DELAY = 0.1 + 0.640; // In seconds
const double FC_SHELL_SPEED = 100.0; // In meters per sec
