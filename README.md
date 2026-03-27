# Sharpie Console
![Sharpie Logo](https://raw.githubusercontent.com/ChristosMaragkos/Sharpie/refs/heads/main/assets/icons/icon_large.png)

[Try the new web-based runner!](https://christosmaragkos.github.io/Sharpie/) | [Join the Discord!](https://discord.gg/M7X58TCV6M)

Sharpie is a 16-bit fantasy console implemented in C#. It is a powerhouse designed to get in your way as little as possible while mimicking how old NES- and SNES-era games were programmed. It features its own custom Assembly language that facilitates most of what you'd need to not pull your hair out in the process of making a game, as well as support for C.

## Hardware Specs
* **CPU:** 16-bit custom little-endian architecture.
* **Registers:** 32 general-purpose registers (Banked into two pages of 16).
* **Memory:** 64KB of addressable space with support for multi-bank cartridge switching.
* **Graphics:** Sprite-based rendering with a hardware camera, a 65536x65536 internal coordinate system, and a dedicated text overlay.
* **Color:** A fixed 32-color palette with support for real-time palette swaps and alternative sub-palettes.
* **Audio:** 8 monophonic channels supporting Square, Triangle, Sawtooth, and Noise waveforms as well as up to 128 distinct instruments.
* **Input:** Native support for up to two players (Keyboard and Gamepad).

## Getting Started
1. **Download:** Grab the latest `sharpie-cli` and `sharpie-runner` from the [Releases](https://github.com/ChristosMaragkos/Sharpie/releases) tab.
2. **Write Code:** Create a `main.c` or `main.asm`.
```c
#include <sharpie.h>

int main(void) {
    print("Hello Sharpie!");
    return 42;
}
```
3. Build: `sharpie main.c -O -o game.shr`
4. Run: Drag and drop `game.shr` onto the Sharpie window or pass its path through the command line.

## License
Sharpie, the Sharpie Logo, and the Sharpie BIOS are licensed under the LGPL License. See [LICENSE.md](https://github.com/ChristosMaragkos/Sharpie/blob/main/LICENSE.md) for details.

*Sharpie (the fantasy console) is in no way affiliated with the actual brand of dry-erase markers.*
