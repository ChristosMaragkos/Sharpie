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

## Support
If you encounter any issues with the Runner or the SDK or have any suggestions, please open an issue over at [the GitHub repository](https://github.com/ChristosMaragkos/Sharpie).

Happy playing and developing!
