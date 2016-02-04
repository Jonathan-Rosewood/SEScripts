# lib #

This directory contains modules that are used in pretty much all/most scripts (except for that one script in `standalone`). Consider them framework/library modules.

## Descriptions ##

  * commons &mdash; Essentially my main library for acquiring references to blocks and block groups. The main benefit of using this rather than `GridTerminalProgram` directly is that the results are cached &mdash; see the `Blocks` and `AllBlocks` properties. This means multiple modules can simply use `Blocks` and/or `AllBlocks` and the underlying GridTerminalProgram method will only be called once (per run of `Main()`).

    Note this library is actually a class and should be instantiated exactly once within the `Main()` method. It *must* be allowed to go out of scope once `Main()` exits.

  * eventdriver &mdash; The "main loop" of my scripts, which allows different modules to be easily integrated. Call `Schedule()` (there are 2 versions) with an `Action<ZACommons, EventDriver>` method and that method will be called sometime in the future once: the requested number of ticks have passed; or, the requested amount of time (in seconds) have passed. `EventDriver` must be associated with a timer block, either implicitly or explicitly (see constructor).
  
    If using an explicitly-assigned timer block (the recommended way), the timer block must run the programming block and do nothing else. This allows `EventDriver` to set the delay itself and to even stop running when idle.

    Otherwise, the timer block must loop on itself using either *Trigger Now* or *Start*. Using *Start* would only work if you never use the tick-delayed version of `Schedule()`.

    The `Main()` method must call the `Tick()` method in the `EventDriver` instance exactly once.

  * gyrocontrol &mdash; Simple API to enable/disable override on gyros and set axis velocity. The relative orientation of the gyros to the local grid does not matter, the API abstracts away yaw/pitch/roll appropriately.
  
  * thrustcontrol &mdash; Simple API to enable/disable thrusters and to set override. Can operate on thrusters on a specific facing (e.g. thrusters that thrust forward) or on all thrusters.
  
  * shiporientation &mdash; API that provides the ship "up" and "forward" directions (relative to the local grid). There are multiple ways to provide a reference block, which is usually done on the first run of `Main()`.

  * shipcontrol &mdash; A superclass of `ZACommons`, this provides lazy-initialized instances of `GyroControl` and `ThrustControl` as well as some reference vectors. Generally, any script that affects flight will use `ShipControlCommons` instead of `ZACommons`.
  
  * pid &mdash; A very simple & reuseable PID (proportional-integrative-derivative) controller.
  
  * rangefinder &mdash; Library to compute the closest point of approach between two line vectors. Often used for rangefinding.
  
  * velocimeter &mdash; Simple library to take rolling position samples and average them into a velocity vector.

  * seeker &mdash; Module that manipulates gyros (yaw & pitch) to keep the ship pointed at a specific relative vector.

## Conventions ##

Modules are integrated into the main loop in one of two ways. They can implement both ways, but only one is used for any given script.

### Direct Integration ###

 * Modules will usually provide an `Init(ZACommons, EventDriver)` method. The signature is not set in stone and can require additional parameters. This is usually called in the "first run" area of the script. This method may schedule something to run in the future or simply initialize the module in some other way. (Note that the `ZACommons` and `EventDriver` instances are typically not available to constructors, hence a separate method for initialization.)
 
 * Zero or more `Run(ZACommons, EventDriver)` methods, if the module will run periodically. Typically scheduled by `Init()` (above) or `HandleCommand()` (below).
 
 * An optional `HandleCommand(ZACommons, EventDriver, string)` method to handle parsing of `Main()`'s `argument` parameter. This is typically called from the `preAction` `Action` of `EventDriver`'s `Tick()`. Like `Init()`, the signature is not set in stone.

Most modules are of this type.

### DockingHandler Integration ###

Modules can simply implement the `DockingHandler` interface.

The script will instantiate the module as an argument to `DockingManager`'s constructor. The `DockingHandler` methods will be called as appropriate whenever the ship docks or undocks. The appropriate method will also be called when the `DockingManager` is first initialized.

See the `safemode`, `batterymonitor`, and `redundancy` modules as examples of this type.

