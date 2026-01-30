.REGION FIXED

LDI r0, 0
MainLoop:
    VBLNK
    INPUT r0, r1

    ICMP r1, 1
    JEQ PlayDing

    ICMP r1, 2
    JEQ PlayShave

    ICMP r1, 4
    JEQ PlayError

    JMP MainLoop

PlayDing:
    LDI r15, BootDing
    SONG r15
    JMP MainLoop

PlayShave:
    LDI r15, ShaveAndAHaircut
    SONG r15
    JMP MainLoop

PlayError:
    LDI r15, ErrorSound
    SONG r15
    JMP MainLoop

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

.ENDREGION
