private readonly BatteryManager batteryManager = new BatteryManager();
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   // SolarGyroController.GyroAxisYaw,
                                                                                   SolarGyroController.GyroAxisPitch,
                                                                                   SolarGyroController.GyroAxisRoll
                                                                                   );
private readonly SafeMode safeMode = new SafeMode();

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    batteryManager.Run(this, ship, argument);
    solarGyroController.Run(this, ship, argument);
    safeMode.Run(this, ship, false);
}
