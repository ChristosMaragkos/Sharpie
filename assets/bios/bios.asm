.INCLUDE "bios_sprites.asm"

.DEF X 100
.DEF Y 100

.ENUM YELLOWS
    YLW_DEF = 5
    YLW_DARK = 21
    YLW_DARKER = 11
.ENDENUM

.ENUM MAGICADDR
    MAGIC_START = $FA20 ; This and the next four bytes should be the ASCII values "SHRP"
    VERSION_ADDR = $FA24
    CART_OK_ADDR = $FA26
    IS_CART_LOADED_ADDR = $FA28
.ENDENUM

.DEF CART_OK 1

.DEF BIOS_VERSION 0x0001 ; Current BIOS Version: 0.1

.DEF CoverLogo 8
.DEF CopyrightSymbol 7

Reset:
    LDI r0, YELLOWS::YLW_DEF
    SWC r0, r0
    LDI r0, 0
    ALT CLS r0

    LDI r0, X
    LDI r1, Y
    LDI r2, 0 ; Start at sprite 0

DrawLogo:
    DRAW r0, r1, r2, r3

    IADD r0, 8
    INC r2 ; move on to the next sprite

    ICMP r2, 7 ; did we just draw sprite 6 (the 'E' in 'SHARPIE')?
    JNE DrawLogo

    LDI 2, CoverLogo
    LDI 0, X

    InitCover: ; Just cover the letters initially
        DRAW r0, r1, r2, r3

        IADD r0, 8

        GETOAM r4
        ICMP r4, 14

        LDI r12, 0
        JNE InitCover


LowerCover:
    LDI r4, 8 ; the first box is in OAM slot 7. The last is in OAM slot 13.
    SETOAM r4
    CLS r12
    LDI r0, X ; reset X
    INC r1 ; Pull down by one pixel

    RedrawCover:
        DRAW r0, r1, r2, r3

        IADD r0, 8

        GETOAM r4 ; Now we can compare OAM slots!

        ICMP r4, 15 ; Did we just alter the sprite at slot 13?
        JNE RedrawCover

        LDI r15, 4
        CALL WaitR15Frames

        ICMP r14, 1 ; did we press the UP key at all?
        JEQ SkipInput
        LDI r13, 0
        INPUT r13, r14

        SkipInput:
            ICMP r1, 108 ; Did we fully uncover the logo?
            JEQ FlashBeep

            JMP LowerCover


FlashBeep:
    ATTR YELLOWS::YLW_DEF
    LDI r0, 14
    IMUL r0, 8
    LDI r1, 31
    IMUL r1, 8
    LDI r2, CopyrightSymbol
    LDI r3, 0
    DRAW r0, r1, r2, r3
    .STR 15, 31 "CHRISTOS MARAGKOS"
    VBLNK

    LDI r0, YELLOWS::YLW_DEF
    LDI r1, 1 ; White
    SWC r0, r1

    ICMP r14, 0b00000001
    JEQ PlayShave

    PlayDing:
        LDI 2 BootDing
        JMP PlaySound

    PlayShave:
        LDI 2 ShaveAndAHaircut

    PlaySound:
        SONG r2

        LDI r15, 4
        CALL WaitR15Frames

        LDI r0, YELLOWS::YLW_DEF
        SWC r0, r0

    LDI r15, 60
    CALL WaitR15Frames

    LDI r0, YELLOWS::YLW_DEF
    LDI r1, YELLOWS::YLW_DARK
    SWC r0, r1

    LDI r15, 30
    CALL WaitR15Frames

    LDI r1, YELLOWS::YLW_DARKER
    SWC r0, r1

    LDI r15, 30
    CALL WaitR15Frames

    LDI r0, 0
    ALT CLS 0

    LDI r15, 60
    CALL WaitR15Frames

    JMP IsRomLoaded

ValidateRom:
    LDI r1, MAGICADDR::MAGIC_START ; Copying so we can iterate
    LDI r2, MagicString ; same here
    LDI r4, 4 ; Using it as a counter should work fine

    MagicCheckLoop:
        ALT LDP r0, r1 ; loaded value goes into r0
        ALT LDP r3, r2 ; correct value goes into r3

        CMP r0, r3

        JNE InvalidCart

        INC r1
        INC r2
        DEC r4

        JNE MagicCheckLoop ; DEC sets the zero flag anyway, no need to ICMP r4, 0

    VersionCheck:
        LDM r0, MAGICADDR::VERSION_ADDR
        LDI r2, BIOS_VERSION

        CMP r0, r2

        JGT BiosTooOld

JMP BootIntoRom

.ENUM CartStatus
    NotLoaded = 0x00
    Loaded = 0x01
    Invalid = 0xFF
.ENDENUM

IsRomLoaded:
    LDI r0, 0
    CLS r0
    ALT LDM r0, MAGICADDR::IS_CART_LOADED_ADDR; Is there a cart loaded?

    ICMP r0, CartStatus::NotLoaded
    JEQ PleaseInsertCart

    ICMP r0, CartStatus::Invalid
    JEQ InvalidCart

    ICMP r0, CartStatus::Loaded
    JEQ ValidateRom

    JMP InvalidCart ; fallback in case the bitrot gremlins visit us

BiosTooOld:
    .STR 8, 13 "Please Update Bios"

InvalidCart:
    CALL DisplayError
    .STR 8, 12 "Invalid Cartridge"

    LDI r0, 0
    STM r0, MAGICADDR::IS_CART_LOADED_ADDR ; Disregard the currently loaded cartridge

    LDI r15, 120
    CALL WaitR15Frames


PleaseInsertCart:
    LDI r0, 0
    CLS r0

    LDI r0, YELLOWS::YLW_DEF
    SWC r0, r0 ; reset color 5 to bright yellow
    ATTR YELLOWS::YLW_DEF ; set text color to it as well

    LDI r0, 0

    BlinkInsertText:
        .STR 5, 12 "Please Insert Cartridge"
        LDI r0, YELLOWS::YLW_DEF

        LDI r1, YELLOWS::YLW_DARKER
        SWC r0, r1
        LDI r15, 10
        CALL WaitR15Frames

        LDI r1, YELLOWS::YLW_DARK
        SWC r0, r1
        LDI r15, 10
        CALL WaitR15Frames

        LDI r0, YELLOWS::YLW_DEF ; gotta reset to 5 since IsRomLoaded overwrites it
        SWC r0, r0
        LDI r15, 10
        CALL WaitR15Frames

        SWC r0, r1
        LDI r15, 10
        CALL WaitR15Frames

        LDI r1, YELLOWS::YLW_DARKER
        SWC r0, r1
        LDI r15, 10
        CALL WaitR15Frames

        LDI r1, 0
        SWC r0, r1
        LDI r15, 20
        CALL WaitR15Frames

    JMP IsRomLoaded

BootIntoRom:
    LDI r0, 0
    ALT CLS r0
    LDI r0, 1
    STM r0, MAGICADDR::CART_OK_ADDR
    JMP $0

DisplayError:
    ATTR 2
    LDI r0, ErrorSound
    SONG r0
    RET

WaitR15Frames: ; KEEP IN MIND that this has the side effect of moving r15 back down to zero. So you must LDI r15, X every time you call it
    VBLNK
    DEC r15
    ICMP r15, 0
    JNE WaitR15Frames
    RET

MagicString:
    .DB 83 ; S
    .DB 72 ; H
    .DB 82 ; R
    .DB 80 ; P

BootDing:
    .DB 0, #C6, 2, 0
    .DB 0, 0, 1, 0
    .DB 0, #G6, 12, 0
    .DW 0xFF, 0

ShaveAndAHaircut:
    .DB 0, #G4 , 12, 0
    .DB 0, #D4 , 6, 0
    .DB 0, #D4 , 6, 0
    .DB 0, #Eb4, 10, 0
    .DB 0, #D4, 12, 0
    .DB 0, 0, 12, 0
    .DB 0, #F#4, 12, 0
    .DB 0, #G4, 6, 0
    .DW 0xFF, 0

ErrorSound:
    .DB 0, #C2, 8, 0
    .DB 0, #C2, 8, 0
    .DW 0xFF, 0
