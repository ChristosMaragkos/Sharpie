# Sharpie Console
![Sharpie Logo](https://raw.githubusercontent.com/ChristosMaragkos/Sharpie/refs/heads/main/assets/icons/icon_large.png)

[Try the new web-based runner!](https://christosmaragkos.github.io/Sharpie/)

Sharpie is a 16-bit fantasy console implemented in C#. It is a powerhouse designed to get in your way as little as possible while mimicking how old NES- and SNES-era games were programmed.
It features its own custom Assembly language that facilitates most of what you'd need to not pull your hair out in the process of making a game.

## Hardware Specs
* **CPU:** 16-bit custom architecture.
* **Registers:** 32 general-purpose registers (Two pages of 16 each).
* **Memory:** 64KB of addressable space.
* **Color:** A 32 color palette with support for palette swaps and alternative palettes
* **Graphics:** Sprite-based rendering with a camera, a 65536x65536 internal display and a text overlay.
* **Audio:** 8 monophonic channels that can play audio simultaneously, with support for **Square, Triangle, Sawtooth** and **Noise** waveforms as well as up to 128 distinct instruments.
* **Input:** Support for up to two players.

## The SDK
The Sharpie SDK handles your entire development pipeline. 

### Features
* **Assembler:** A lightning-fast CLI (and now a GUI) that turns `.asm` code into `.shr` cartridges.
* **Syntax Highlighting:** First-class support for Neovim and VS Code (located in `tools/editor-support`).

## Getting Started
1.  **Grab the Release:** Download the build you need from the [Releases](https://github.com/ChristosMaragkos/Sharpie/releases) tab.
2.  **Setup your Editor:** Follow the instructions in `tools/editor-support` to get those opcodes looking pretty.
3.  **Write Code:**
    ```asm
    .INCLUDE "my_sprites.asm"
    
    LDI r1, $01       ; Load a value
    OUT_R r1          ; Print it to the console
    ```
4.  **Assemble:** `Sharpie.Sdk -i mygame.asm`
5.  **Run:**
    Drag your `.shr` file onto the Sharpie Runner and watch the magic happen.

### Developer Support
If you'd like to ask something specific about the Sharpie, feel free to open an [Issue](https://github.com/ChristosMaragkos/Sharpie/issues) or a [Discussion](https://github.com/ChristosMaragkos/Sharpie/discussions). The [Wiki](https://github.com/ChristosMaragkos/Sharpie/wiki) also contains much more nuanced info about the console's architecture.

##  Repository Structure
* `/src`: The C# source code for the Sharpie as well as the runner interfaces.
* `/tools`: The SDK, Assembler, and Editor support files.
* `/assets`: Logos, icons, and release materials.

## License
Sharpie, the Sharpie Logo and the Sharpie BIOS are all licensed under the LGPL License, and the Sharpie SDK uses ImageSharp for its PNG->ASM conversion. See [LICENSE.md](https://github.com/ChristosMaragkos/Sharpie/blob/main/LICENSE.md) for the boring legal details.

### But, can I sell my Sharpie game?
There are plans for standalone self-contained Sharpie ROM support by packaging them with a runner of your choice. No ETA on that yet, but stay tuned.
**However:**

The intent is to enable the creation and distribution of games.

While third-party runners are more than welcome, we discourage the monetization of standalone runner implementations
as we believe the ecosystem works best when commercial value is centered on games rather than platforms.
