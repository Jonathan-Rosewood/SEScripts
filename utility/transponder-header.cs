// Transponder

// Transmit settings
// If you want to transmit, add the line "transponderID <id>" to the
// prog block's Custom Data.
const double TRANSPONDER_UPDATE_RATE = 1.0; // In seconds
const MyTransmitTarget TRANSPONDER_TARGET = MyTransmitTarget.Default;

// Receive settings
const string TRANSPONDER_TIMEOUT = "01:00"; // HH:MM[:SS]
