// ProNavGuidance
const double PN_GUIDANCE_GAIN = 5.0;

// ProNav typically sucks when the target is at extreme angles.
// The following will point the missile directly at the target immediately
// after launch.
const double ONE_TURN_DURATION = 2.0; // In seconds
