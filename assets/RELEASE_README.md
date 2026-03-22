# Sharpie Console v0.2 (Beta)

Thank you for downloading the Sharpie fantasy console! No, seriously, your support means everything.
The program you have downloaded is a *runner* for the Sharpie console, specifically built on Raylib and tested extensively. It allows up to two players and has full controller support.

## Getting Started

1. **Windows:** Run the `.exe`.
2. **Linux:** Run the program (maybe you will need to `chmod +x` it), or fill in the paths of the included `.desktop` file and run that.
3. **MacOS:** MacOS is currently not supported, but it might be in the future. Stick around!

All necessary assets are included either in the `zip` you just downloaded or embedded into the executable itself, so no need to worry about dependencies!

## Controls (Keyboard/Joypad)
Currently, the keyboard controls Player 1, and the first detected controller controls Player 2, with the second one going to Player 1 as well. The control scheme is as follows:

- **D-pad:** Arrow Keys / Left Joystick or controller d-pad if you like to live on the edge
- **A:** Z / XBox B or PlayStation circle or Nintendo A
- **B:** X / XBox A or PlayStation X or Nintendo B
- **Start:** Left Shift / The right middle button (PlayStation Start, Switch Pro Controller +)
- **Option:** Tab / The left middle button
- **Reset:** Currently no reset button.

## Loading ROMs
To load a ROM, you can either start the Sharpie by dragging the `.shr` file on it or start the program and then drag the ROM in. However, to load another cartridge, you must restart the console.

## Developing ROMs
The SDK now has a dedicated GUI and the CLI mode got a major overhaul: You now use project manifests in JSON format to assemble ROMs. In these manifests you define:

- The palette
- Title & author metadata
- Whether you're compiling as firmware
- Input & output paths

You can also edit those fields manually from the GUI.

As of 0.3, you can also write games in C and compile them to Sharpie Assembly using the bundled `sharpiecc` and `sharpie.h`, but you must install LLVM like so:

- Windows: `winget install LLVM`
- Linux: `<your-package-manager> install llvm`

## Using the C Compiler (sharpiecc)

Once LLVM is installed, you can compile your C code directly into playable Sharpie ROMs (or raw assembly) from the command line.

Basic Compilation:
```sh
sharpiecc main.c -O -o mygame.bin
```

Compiler Flags:

    -O: Enables the Peephole Optimizer and Parameter Promotion. (Highly recommended for performant games!)

    -S: Emits readable Sharpie Assembly (.asm) instead of compiling directly to a binary. Great for debugging or learning the ISA.

    -o <file>: Specifies the output file name.

Multi-Bank Projects:
Sharpie games can span multiple files and memory banks. Simply pass all your .c files to the compiler at once:

```sh
sharpiecc main.c player.c level1.c -O -o mygame.bin
```

To assign a file to a specific memory bank, just add #pragma bank X (e.g., #pragma bank 1) to the very top of your .c file. The compiler handles all the cross-bank routing automatically!

## Support
If you encounter any issues with the Runner or the SDK or have any suggestions, please open an issue over at [the GitHub repository](https://github.com/ChristosMaragkos/Sharpie).

Happy playing and developing!
