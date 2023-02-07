# H3MP - Multiplayer mod for H3VR

As the title says, this is a mod that adds multiplayer to the virtual reality game **Hotdogs, Horseshoes and Hangrenades**.

### Installation

1. Download install the latest version of [BepinEx](https://github.com/BepInEx/BepInEx/releases)
2. Download the [latest release of H3MP](https://github.com/TommySoucy/H3MP/releases)
3. Put both H3MP.dll and the H3MP folder from the zip file into the plugins folder (H3VR\BepInEx\plugins)

### Usage

All H3MP functions can be access through the wristmenu, in-game (apart from the settings in the config file, see Config section below).

To join a server, you will have to have set the IP and port of the server in your config file (see Config section below), then, in the wristmenu, press H3MP->Join

**_If you forgot to set these before going in-game and restarting the game is too much of a hassle, mainly for people with 200+ mods that I figured probably take a while to load, you can set your config, then go to H3MP->Options->Reload config in the wristmenu which will reload your configs, after which you can connect/host_**

#### Options

In the wristmenu, some options are available:

**_Reload config_**: Explained above, will reload the config file if you modified it since starting the game. Useful if yo modified it and don't want to have to restart the game.

**_Item interpolate_**: This will toggle item interpolation. Item interpolation is the smoothing of item movement to prevent everything from looking "jagged". Turning it off will ensure that items are positionned/rotated exactly as you receive the data from another client. Due to latency, this will make item movement look extremely low FPS.

#### Config

The **config** refers to a file in the H3MP folder called **Config.json** which contains a few important settings.

**_IP_**: The IP of the server you will be joining **or hosting from**
**_Port_**: The port of the server you will be joining **or hosting from**
**_MaxClientCount_**: The maximum number of clients that can connect to your server
**_Username_**: The username you will have on the server

### Hosting

To host a server on a **local network**, you can set your **IP** as your machine's local IP address, which can be found by running the "ipconfig" command in CMD.

To host a server **for public access**, you will need to set your **IP** as your machine's **public** IP address, which can be found by searching "what is my IP" on google.
For public access, you will need to **port foward** the port you have set in your config.

To start the server, in the wristmenu, press H3MP->Host
