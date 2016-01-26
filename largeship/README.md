# largeship #

Modules dealing with blocks or systems typically only found on large grids.

## Descriptions ##

 * complexairlock &mdash; Code to control "full" airlocks (ones with air vents). See [Ship Manager][shipmanager] for usage documentation. It's quite a mess because it was probably my first complex C# program.
 
 * doorautocloser &mdash; Automatically closes open doors after some amount of time.
 
 * oxygenmanager &mdash; Selectively enables/disables oxygen generators and oxygen farms depending on the average level of tanks. Note that the addition of hydrogen breaks this somewhat, since only oxygen generators create hydrogen and AFAIK, there's no l10n-independent method of distinguishing oxygen tanks and hydrogen tanks.
 
 * productionmanager &mdash; Given an array of assemblers tagged a specific way, selectively enables/disables them to maintain a stock of components.
 
 * refinerymanager &mdash; Attempts to keep all refineries active by splitting stacks of ore.
 
 * simpleairlock &mdash; For "simple" airlocks (ones that are simply two doors), it attempts to prevent more than one door from opening at a time.
 
 * timerkicker &mdash; Simply "kicks" (invokes the "Start" action) all timer blocks named a specific way on the local grid and attached grids. Solves a fairly niche problem that I see where timer blocks stop once connectors lock/unlock. Not really needed much anymore since my drone/utility ship scripts halt when docked.

All of these modules are currently released as my [Ship Manager script][shipmanager].

[shipmanager]: https://steamcommunity.com/sharedfiles/filedetails/?id=474902825
