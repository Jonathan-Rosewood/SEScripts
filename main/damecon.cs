//! Damage Control
//@ commons eventdriver damagecontrol
public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly DamageControl damageControl = new DamageControl();

void Main(string argument)
{
    var commons = new ZACommons(this);

    eventDriver.Tick(commons, preAction: () => {
            damageControl.HandleCommand(commons, eventDriver, argument);
        });
}
