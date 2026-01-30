.ENUM Buttons
    Up = 0x01
    Down = 0x02
    Left = 0x04
.ENDENUM

.REGION FIXED
.STR 9, 15 "PRESS ANY BUTTON"

MainLoop:
    LDI r0, 0
    INPUT r0, r1

    ICMP r1, Buttons::Up
    JEQ ManualTrigger

    ICMP r1, Buttons::Left
    JEQ IllegalOamCursor

    ICMP r1, Buttons::Down
    JEQ ProtectedRegionWrite

    JMP MainLoop

ManualTrigger: ; Deliberately trigger a segfault
    ALT HALT

IllegalOamCursor: ; Trigger a segfault by setting the OAM cursor to a value outside its valid range
    LDI r0, 0xFFFF
    SETOAM r0
    HALT

ProtectedRegionWrite: ; Trigger a segfault by trying to write to the reserved RAM region
    LDI r0, 0xFF
    STM r0, $FFFF
    HALT
.ENDREGION
