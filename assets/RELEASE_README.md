# Sharpie Console v0.4

Thank you for downloading the Sharpie fantasy console! No, seriously, your support means everything.

## Getting Started

1. **Windows:** Run `Sharpie.exe`.
2. **Linux:** Run the `Sharpie` binary (you may need to `chmod +x` it), or use the included `.desktop` file.
3. **MacOS:** MacOS is currently not supported. Stick around!

All necessary assets are included either in the package you just downloaded or embedded into the executable itself, so no need to worry about dependencies!

## Controls (Keyboard/Joypad)
Currently, the keyboard controls Player 1, and the first detected controller controls Player 2.

- **D-pad:** Arrow Keys / Left Joystick or Controller D-pad
- **A:** Z / Xbox B / PlayStation Circle / Nintendo A
- **B:** X / Xbox A / PlayStation X / Nintendo B
- **Start:** Left Shift / Start / Options / +
- **Option:** Tab / Back / Select / -
- **Reset:** Currently no reset button.

## Loading ROMs
To load a ROM, you can either drag the `.shr` file onto the Sharpie executable or start the program and drag the ROM into the window.
Alternatively, you can pass in a file path through the command line like so: `Sharpie path/to/your/cartridge.shr`

However, to load a different cartridge, you must restart the console.

## Developing ROMs (The Sharpie CLI)
As of v0.4, the legacy SDK and the C compiler have been unified into a single tool: `sharpie`.

### Using the CLI
The `sharpie` tool handles C compilation, assembly, and ROM exporting in one go. It decides what to do based on your file extensions.

**Basic C Compilation:**
```sh
sharpie main.c -o game.shr
```

**Assembling raw ASM files:**
```sh
sharpie bios.asm -f -o bios.bin
```

- CLI Flags:

    Inputs: You can pass multiple .c files at once to compile them into a single ROM, or a single .asm file.

    - -o <file>: Specifies the output path.

    - -O: Enables C optimizations. Defaults to off.

    - -S: Stops after compiling C and emits an assembly file.

    - -f: Firmware mode. Assembles a raw binary without the standard .shr headers.

    - -t \<title\>: Sets the internal ROM title (visible in the header).

    - -a \<author\>: Sets the internal ROM author name.

## Support
If you encounter any issues with the Runner or the CLI, please open an issue over at [the GitHub repository](https://github.com/ChristosMaragkos/Sharpie).
Happy playing and developing!
