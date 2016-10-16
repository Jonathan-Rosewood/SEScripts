// VTVLHelper
// Drop
const Base6Directions.Direction VTVLHELPER_BURN_DIRECTION = Base6Directions.Direction.Forward;
const double VTVLHELPER_BURN_SPEED = 98.0; // In meters per second
const Base6Directions.Direction VTVLHELPER_BRAKE_DIRECTION = Base6Directions.Direction.Down; // Direction to face toward planet
const double VTVLHELPER_BRAKING_SPEED = 50.0; // In meters per second
const string VTVLHELPER_DROP_DONE = "Drop Done";
// Launch
const Base6Directions.Direction VTVLHELPER_LAUNCH_DIRECTION = Base6Directions.Direction.Up;
const double VTVLHELPER_LAUNCH_SPEED = 98.0; // In meters per second
const string VTVLHELPER_LAUNCH_DONE = "Launch Done";
// Autodrop
const double VTVLHELPER_APPROACH_GAIN = 0.1; // Multiplied by distance to get approach speed
const double VTVLHELPER_MINIMUM_SPEED = 5.0; // In meters per second
// Orbit
const Base6Directions.Direction VTVLHELPER_ORBIT_DIRECTION = Base6Directions.Direction.Down;
