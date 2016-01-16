# ZerothAngel's Space Engineers Scripts #

Space Engineers scripts are already open source in nature, so while releasing
these scripts may seem a bit redundant, they will at least be easier to edit
and/or manage by others. (Additionally, when I release on [Steam Workshop](https://steamcommunity.com/id/ZerothAngel/myworkshopfiles/?appid=244850),
the scripts often have leading whitespace stripped to save space.)

**Note that I don't really play Space Engineers anymore**. At around the 01.100
release, Keen started breaking things left and right. Sometimes, it would
be the "hardware" &mdash; e.g. oxygen tanks and such. Then in 01.102, they
utterly broke the "software" side by removing the `IMyPowerProducer`
interface and then breaking certain generic language constructs.

Nowadays (01.117) things actually seem fine on the normal gameplay side.
But the scripting API seems to have been left to rot (and there's no sign of
it on the recently released roadmap).

So I'll be moving on. But I release my scripts in their "raw" form with
hopes that they'll be useful to someone.

Though if Keen ever reworks their API &mdash; and makes scripting more
like programming your ship rather than programming their game &mdash;
I'll be back. ;)

## Structure ##

The scripts are actually divided into "modules."

The actual scripts that you load into the game are made by concatenating
one or more modules.

Which scripts to concatenate are defined by "spec" files.

Modules can have optional "header" and "footer" (a misnomer) files, which
are concatenated at different places.

The concatenation order is defined as:

  * Any and all -header.cs files, in order
  * Any and all -footer.cs files, in reverse order
  * All specified .cs files, in order

Modules are organized into directories as follows:

  * `largeship` &mdash; For blocks/systems normally only found on large grids
  * `lib` &mdash; Library modules, generally used by most scripts
  * `main` &mdash; The "main" method and backbone for each script
  * `misc` &mdash; One-off modules used for specific purpose (e.g. state machines and such)
  * `standalone` &mdash; Standalone scripts that don't require any other module
  * `utility` &mdash; Bulk of reusable modules, which usually deal with a single system
  * `weapon` &mdash; Weapon-related stuff, mostly for guided missiles

Note that power related modules (especially battery and solar modules) are
currently broken.

## Building ##

The build script is a Python 3 script named `build.py`.

Simply give it the path to one or more *.spec files (such as those found
in the `specs` directory).

I typically run it like so

    ./build.py -w specs/*.spec

and then copy the contents of the `out` directory to my
`%APPDATA%/SpaceEngineers/IngameScripts/local/` directory.
