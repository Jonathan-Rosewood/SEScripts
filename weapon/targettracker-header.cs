// TargetTracker
const string MAIN_CAMERA_GROUP = "MainCamera";
const string TARGET_UPDATE_PREFIX = "TrackerUpdates";
const MyTransmitTarget TRACKER_ANTENNA_TARGET = MyTransmitTarget.Default;

const double INITIAL_RAYCAST_RANGE = 10000.0; // In meters
const double RAYCAST_RANGE_BUFFER = 1.25; // Should be >= 1.0
const double TRACKER_UPDATE_RATE = 0.5; // In seconds
const double TRACKER_REFRESH_RATE = 1.0; // In seconds

const string TRACKER_PING_GROUP = "TrackerPing";
const string TRACKER_MISS_GROUP = "TrackerMiss";
