# Steam Game Recording Integration

This Celeste mod provides an integration with steam's [Game Recording
feature](https://store.steampowered.com/gamerecording).

Specifically, it allows automatically adding markers to the timeline for deaths,
and clippable highlights for successful screen attempts.

## Installation

Currently, installation of this mod is a bit involved, as it requires manually
updating the version of steam_api and Steamworks.NET provided by Celeste.

1. Download the Standalone zip of [Steamworks.NET release 2024.8.0]
   Do not download a newer version, as it is incompatible with Celeste!
2. Copy the appropriate Steamworks.NET.dll file in your Celeste folder,
   overwriting the vanilla version.
3. Copy the appropriate steam_api file to the native lib location:
    - On windows 32-bit, copy steam_api.dll to Celeste\lib64-win-x86
    - On windows 64-bit, copy steam_api64.dll to Celeste\lib64-win-x64
    - On MacOS, copy libsteam_api.dylib (found deep in the .bundle folder) to
      Celeste\lib64-osx
    - On linux, copy libsteam_api.so to Celeste\lib64-linux-x64

Finally, install this mod by copying the Release ZIP in your Mods folder.

> [!NOTE]
> We are working with Everest to simplify the installation instructions.
> Hopefully, this mod should soon be as simple to install as any other mod.

[Steamworks.NET release 2024.8.0]: https://github.com/rlabrecque/Steamworks.NET/releases/tag/2024.8.0