# OpenLegoBattles
OpenLegoBattles (OLB) is a remake of the Nintendo DS game Lego Battles, recreated in C# using MonoGame.

Note that no game assets are included in this repo, you require the rom file to be able to use this software.

## Setup
Once you have the project cloned, set the startup project to ContentUnpacker, the configuration to Release, and build. You can then set the project back to OpenLegoBattles and configuration back to Debug.

The built ContentUnpacker is copied into the game's folder automatically when it is built, as it is needed by the game. It will take the release build for extra speed while unpacking.

Upon starting the OpenLegoBattles project for the first time, it will ask for a Lego Battles nds file. Simply drag and drop the file onto the game window and it will begin extracting the assets. After a short while, the logo will appear and a map will be displayed.

--noIntro can be added to the command line arguments to skip the logo and go straight to the map. Note that this also skips the check for the game content existing and will instead throw an error.

Deleting the BaseGame folder in the Content folder of the built project (under the bin folder) will cause the program to ask for the nds file again when next run. This can be used when testing the content unpacker tool.
