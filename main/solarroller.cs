private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   SOLAR_GYRO_GROUP,
                                                                                   SolarGyroController.GyroAxisRoll
);

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    solarGyroController.Run(this, ship, argument);
}
