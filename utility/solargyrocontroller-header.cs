// SolarGyroController
const float SOLAR_GYRO_VELOCITY = 0.05f; // In radians per second
const double SOLAR_GYRO_AXIS_TIMEOUT = 15.0; // In seconds
const float SOLAR_GYRO_MIN_ERROR = 0.005f; // As a fraction of theoretical max
// If you're using modded solar panels, set these values (both in MW)
const float SOLAR_PANEL_MAX_POWER_LARGE = 0.120f; // Max power of large panels
const float SOLAR_PANEL_MAX_POWER_SMALL = 0.030f; // Max power of small panels
