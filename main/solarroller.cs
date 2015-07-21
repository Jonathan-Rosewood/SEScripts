private readonly SolarGyroController solarGyroController = new SolarGyroController(GyroControl.Roll);

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    solarGyroController.Run(this, ship, argument);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
