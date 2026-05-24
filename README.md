# Wally Anm Renderer. An external animation renderer for Brawlhalla.

<img width="1366" height="705" alt="screenshot000" src="https://github.com/user-attachments/assets/24b2ea52-0c14-4e85-aff7-27a1b52a3380" />

#### Existing features:
* Load any .anm files and pick an animation.
* Select any of many parameters that affect the animation:
  * Legend skin
  * Weapon skin
  * Held item
  * Sidekick
  * Companion
  * Color
  * Podium
  * Loading frame
  * Insignia
  * Avatar
  * Emoji
  * End of match fanfare
  * Horde enemies
  * Volleybrawl ball colors
  * Bubble tag bubble colors
* A full implementation of the game's animation system. Including support for asymmetry.
* Pause, frame step, and seeing exact animation timeline.
* Support for animation loop points.
* Listing the sprites and their data.
* Seeing both the internal and the ingame name of various items.
* Creating custom colors.
* Exporting an animation frame as svg or png.
* Exporting an animation as a seuquence of svg or png images.
* Exporting an animation as gif, webp, or apng

#### Planned features:
* Bulk exporting of many animations.
* Correct timing for attack animations (harder than it sounds).

#### Unsupported swf features:
* Color transforms are only supported on export, not during rendering. This is only used for the brawlball goal.
* Bitmap fill styles (rarely used in the game, but used by some mods to get around color swapping limitations).

## Download
Download the latest release [here](https://github.com/allhailcheese/WallyAnmRenderer/releases/latest)

If the program doesn't launch on windows, you may need to install the [latest Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

### Building from source

make sure you have _git_ installed.

download the code:

`git clone --recurse-submodules https://github.com/allhailcheese/WallyAnmRenderer.git`

run the project (inside the WallyAnmRenderer folder created by git clone):

`dotnet run --project WallyAnmRenderer`

## Requirements

- .NET 8 SDK (if building from source)
- On Windows: latest Visual C++ Redistributable
- A Brawlhalla installation

## Submodules

- BrawlhallaAnimLib - C# library implementing Brawlhalla's animation logic.

- WallyAnmSpinzor - C# library for parsing Brawlhalla .anm files.

- BrawlhallaSwz - C# library for encrypting and decrypting Brawlhalla .swz files.

- SwiffCheese - C# library for converting flash vector graphics into svg.

- AbcDisassembler - C# library for parsing actionscript bytecode. Used to find the swz decryption key and the .a array for sprites.

- BrawlhallaLangReader - C# library for parsing Brawlhalla language .bin files.

## Package Dependencies

This list includes the dependencies from submodules.

- Magick.NET (14.12.0) - Library for image manipulation. Used for svg rasterization.

- NativeFileDialogSharp (0.5.0) - C# bindings for nativefiledialog, a C library for opening the platform's default file explorer dialog.

- Raylib-cs (7.0.2) - C# bindings for Raylib, a C rendering library.

- ImGui.NET (1.91.6.1) - C# bindings for ImGui, a C++ ui library.

- rlImgui-cs (3.2.0) - C# library for bridging between Raylib-cs and ImGui.NET.

- SwfLib (1.0.5) - C# library for parsing .swf files.

- OneOf (3.0.271) - C# tagged union library.

- Sep (0.12.3) - C# csv parsing library.
