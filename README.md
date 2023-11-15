# H3MP - Multiplayer mod for H3VR

As the title says, this is a mod that adds multiplayer to the virtual reality game **Hotdogs, Horseshoes and Handgrenades**.

## IMPORTANT

- See incompatibilities section below for a list of known incompatible mods and modes.
- Only report bugs if you don't use any of the mods from the incompatibilities list.
- Report bugs on the H3VR homebrew discord server's #h3mp-general channel and always send **_full_** output logs.
- Will most probably work with the Main version of the game, but currently only guaranteed to work with "alpha - Mod Safe".
- If you get "PatchVerify" errors on start up it means either your game or H3MP is the wrong version.
- Port forward or hosting solution must support both TCP and UDP

## Manual Installation

1. Download and install the latest (**_not pre-release_**) version of [BepinEx](https://github.com/BepInEx/BepInEx/releases)
2. Download the [latest release of H3MP](https://github.com/TommySoucy/H3MP/releases)
3. Put the H3MP folder from the zip file into the plugins folder (H3VR\BepInEx\plugins)

So you should end up with a H3VR\BepInEx\plugins\H3MP folder, containing H3MP.dll and other files.

## Automatic Installation

Can be done through thunderstore using [r2modman mod manager](https://h3vr.thunderstore.io/package/ebkr/r2modman/)

## Usage

All H3MP functions can be accessed through the wristmenu, in-game (apart from the settings in the config file, see Config section below).

To join a server, you will have to have set the IP and port of the server in your config file (see Config section below), then, in the wristmenu, press H3MP->Join

**_If you forgot to set these before going in-game and restarting the game is too much of a hassle, mainly for people with 200+ mods that I figured probably take a while to load, you can set your config, then go to H3MP->Options->Reload config in the wristmenu which will reload your configs, after which you can connect/host_**

### Options

In the wristmenu, some options are available:

- **_Reload config_**: Will reload the config file if you modified it since starting the game. Useful if you modified it and don't want to have to restart the game.

- **_Item interpolate_**: This will toggle item interpolation. Item interpolation is the smoothing of item movement to prevent everything from looking "jagged". Turning it off will ensure that items are positionned/rotated exactly as you receive the data from another client. Due to latency, this will make item movement look extremely low FPS.

- **_TNH Revive_**: Will revive you if you are in MP TNH. This is so you can rejoin an ongoing game without having to restart it. Also useful if wanting to use a TNH map similarly to a sandbox map but dying respawns you in the wrong place. This will respawn you at your spawn supply point and get rid of your dead and spectating status.

- **_Current color_**: Lets you go through certain colors of player model. This color will be visible to other players. Available colors: White, Red, Green, Blue, Black, Desert, and Forest. Note that Desert and Forest are not camo they're just Beige and Dark green. **Only available while connected!**

- **_Current IFF_**: Lets you change IFF. Note that friendly fire is currently always on. **Only available while connected!**

- **_Color by IFF_**: Toggle that lets the host decide whether player colors should correspond to IFF. IFF 0 corresponds to White, 1 to Red, 2 to Green, and so on. Sequence is same as in **_Current color_** option. The color then loops, so IFF 7 will correspond to White. **Only available to Host!**

- **_Nameplate mode_**: Lets host decide in which case the nameplate and health of a player should be visible. Available modes are: All, Friendly Only (Default), and None. All means all nameplates will always be visible to everyone, Friendly Only means nameplates will only be visible to player with same IFF, and None means no nameplates will be visible, no matter the IFF. **Only available to Host!**

- **_Radar mode_**: Lets host decide which players should be visible on the TNH radar. Available options are: All (Default), Friendly Only, and None. All will show both friendly and enemy players, Friendly Only will show only friendly players (same IFF), and None will show no other player, regardless of IFF. **Only available to Host!**

- **_Radar Color IFF_**: Lets host decide which colors players should have on the TNH radar. If true it will show players as Green or Red, depending on whether they are Friendly or Enemy, respectively. If false, it will instead show players with their corresponding colors. **Only available to Host!**

- **_Max health_**: Lets host decide the max player health in their current scene/instance. Available options are: Unset (default), 1, 500, 1000, 2000, 3000, 5000, 7500, and 10000. Unset means that the maximum health will be what was set by vanilla. **Only available to Host!**

### Config

The **config** refers to a file in the H3MP folder called **Config.json** which contains a few important settings.

- **_IP_**: The IP of the server you will be joining
- **_Port_**: The port of the server you will be joining **or hosting from**
- **_MaxClientCount_**: The maximum number of clients that can connect to your server
- **_Username_**: The username you will have on the server

## Hosting

To host a server on a **local network**, anyone who wants to connect to you will have to set their config's **IP** to your machine's local IP address, which can be found by running the "ipconfig" command in CMD.

To host a server **for public access**, anyone who wants to connect to you will have to set their config's **IP** to your machine's **public** IP address, which can be found by searching "what is my IP" on google.

For public access, you will need to **port foward** the port you have set in your config. **_Important: Port forward needs to be both TCP and UDP._**

In both cases, both host and client will have to set their port to the portforwarded one.

The default port of 7861 can be used safely (Unless you already have something running on your machine that uses it), though most numbers between 0 and 65535 should work fine apart from very specific ones. If you want to use a different one, look it up online first to see if it is reserved for anything else. Some specific ones may also be blocked by your ISP.

To start the server, in the wristmenu, press H3MP->Host

## Incompatibilities

- Freemeat mode and entities
- Meat fortress mode
- Rotweiners mode
- Anything not sandbox or TNH will most probably not work

## Upcoming support

- Escape from Meatov
- Meat fortress mode
- Rotweiners mode
- Freemeat mode and entities
