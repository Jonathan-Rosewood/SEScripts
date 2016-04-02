//@ commons eventdriver
public interface DockingHandler
{
    void PreDock(ZACommons commons, EventDriver eventDriver);
    void DockingAction(ZACommons commons, EventDriver eventDriver,
                       bool docked);
}
