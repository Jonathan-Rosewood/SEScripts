private readonly BatteryManager batteryManager = new BatteryManager();
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   // SolarGyroController.GyroAxisYaw,
                                                                                   SolarGyroController.GyroAxisPitch,
                                                                                   SolarGyroController.GyroAxisRoll
                                                                                   );

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    batteryManager.Run(this, ship);
    solarGyroController.Run(this, ship, argument);
}
