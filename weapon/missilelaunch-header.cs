// Missile group suffix (e.g. " #1", " #2", etc.)
const string MISSILE_GROUP_SUFFIX = "";

// Detachment burn (optional)
const bool DETACH_BURN = true; // Whether to first burn away from launcher
const Base6Directions.Direction DETACH_BURN_DIRECTION = Base6Directions.Direction.Down; // Direction to burn to detach
const double DETACH_BURN_TIME = 2.0; // In seconds

// Initial forward burn before handing control over to guidance
const double BURN_FRACTION = 0.25; // Total force as a fraction of max forward thrust
const double BURN_TIME = 1.0; // In seconds
