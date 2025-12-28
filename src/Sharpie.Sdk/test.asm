; SHARP-16 FIRST OFFICIAL ROM

; Test constants (.DEF)
.DEF PLAYER_X 100
.DEF PLAYER_Y 50
.DEF WAITING $FFFF

; Test code generation
Start:
    LDI r0, PLAYER_X
    LDI r1, PLAYER_Y

    ; Label resolution test (do we jump?)
    JMP MainLoop

; Test .STR expansion (should generate multiple TEXT instructions)
.STR 10, 10, "HELLO WORLD"

MainLoop:
    ; Test backward reference
    JMP MainLoop

MusicData:
    .DB C4, D4, E4 ; Random notes
    .DB FF ; Song terminator

.SPRITE 0
; Simple square
    .DB 11, 11, 11, 11
    .DB 11, 11, 11, 11
    .DB 11, 11, 11, 11
    .DB 11, 11, 11, 11
; Top half color 1, bottom half color 2
    .DB 12, 12, 12, 12
    .DB 12, 12, 12, 12
    .DB 12, 12, 12, 12
    .DB 12, 12, 12, 12
