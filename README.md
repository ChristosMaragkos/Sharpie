# Sharpie Console
![Sharpie Logo](https://raw.githubusercontent.com/ChristosMaragkos/Sharpie/refs/heads/main/assets/icons/icon_large.png)

Sharpie is a 16-bit fantasy console implemented in C#. It is a powerhouse designed to get in your way as little as possible while mimicking how old NES- and SNES-era games were programmed. 

No high-level scripting languages. No modern luxuries. Just you and the CPU.

## Hardware Specs
* **CPU:** 16-bit custom RISC architecture.
* **Registers:** 16 general-purpose registers (R0-R15).
* **Memory:** 64KB of addressable space.
* **Color:** An internal 32-color palette and 16 active colors at any time, with support for color swapping.
* **Graphics:** Sprite-based rendering with a 256x256 internal display and a text overlay.
* **Audio:** 8 monophonic channels that can play audio simultaneously, with support for **Square, Triangle, Sawtooth** and **Noise** waveforms as well as up to 128 distinct instruments.
* **Input:** Support for up to two players.

## The SDK
The Sharpie SDK handles your entire development pipeline. 

### Features
* **Assembler:** A lightning-fast CLI (and soon™ a GUI) that turns `.asm` code into `.shr` cartridges.
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

##  Repository Structure
* `/src`: The C# source code for the Sharpie as well as the runner interfaces.
* `/tools`: The SDK, Assembler, and Editor support files.
* `/assets`: Logos, icons, and release materials.

## License
Sharpie, the Sharpie Logo and the Sharpie BIOS are all licensed under the LGPL License. See [LICENSE.md](https://github.com/ChristosMaragkos/Sharpie/blob/main/LICENSE.md) for the boring legal details.

### But, can I sell my Sharpie game?
There are plans for standalone self-contained Sharpie ROM support by packaging them with a runner of your choice. No ETA on that yet, but stay tuned.
**However:**

The intent is to enable the creation and distribution of games.

While third-party runners are more than welcome, we discourage the monetization of standalone runner implementations
as we believe the ecosystem works best when commercial value is centered on games rather than platforms.
