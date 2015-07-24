private readonly PowerManager powerManager = new PowerManager();

void Main(string argument)
{
    var ship = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(ship);

    powerManager.Run(this, ship);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
