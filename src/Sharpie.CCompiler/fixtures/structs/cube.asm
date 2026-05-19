; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: cube.c
; ----------------------------------

.REGION FIXED
; Global Variables
.GLOBAL
_global_sin_table:
    .DW 0
    .DW 6
    .DW 12
    .DW 18
    .DW 25
    .DW 31
    .DW 37
    .DW 43
    .DW 49
    .DW 56
    .DW 62
    .DW 68
    .DW 74
    .DW 80
    .DW 86
    .DW 92
    .DW 97
    .DW 103
    .DW 109
    .DW 115
    .DW 120
    .DW 126
    .DW 131
    .DW 136
    .DW 142
    .DW 147
    .DW 152
    .DW 157
    .DW 162
    .DW 167
    .DW 171
    .DW 176
    .DW 180
    .DW 185
    .DW 189
    .DW 193
    .DW 197
    .DW 201
    .DW 205
    .DW 208
    .DW 212
    .DW 215
    .DW 219
    .DW 222
    .DW 225
    .DW 228
    .DW 231
    .DW 233
    .DW 236
    .DW 238
    .DW 240
    .DW 242
    .DW 244
    .DW 246
    .DW 247
    .DW 249
    .DW 250
    .DW 251
    .DW 252
    .DW 253
    .DW 254
    .DW 255
    .DW 255
    .DW 256
    .DW 256
    .DW 256
    .DW 255
    .DW 255
    .DW 254
    .DW 253
    .DW 252
    .DW 251
    .DW 250
    .DW 249
    .DW 247
    .DW 246
    .DW 244
    .DW 242
    .DW 240
    .DW 238
    .DW 236
    .DW 233
    .DW 231
    .DW 228
    .DW 225
    .DW 222
    .DW 219
    .DW 215
    .DW 212
    .DW 208
    .DW 205
    .DW 201
    .DW 197
    .DW 193
    .DW 189
    .DW 185
    .DW 180
    .DW 176
    .DW 171
    .DW 167
    .DW 162
    .DW 157
    .DW 152
    .DW 147
    .DW 142
    .DW 136
    .DW 131
    .DW 126
    .DW 120
    .DW 115
    .DW 109
    .DW 103
    .DW 97
    .DW 92
    .DW 86
    .DW 80
    .DW 74
    .DW 68
    .DW 62
    .DW 56
    .DW 49
    .DW 43
    .DW 37
    .DW 31
    .DW 25
    .DW 18
    .DW 12
    .DW 6
    .DW 0
    .DW -6
    .DW -12
    .DW -18
    .DW -25
    .DW -31
    .DW -37
    .DW -43
    .DW -49
    .DW -56
    .DW -62
    .DW -68
    .DW -74
    .DW -80
    .DW -86
    .DW -92
    .DW -97
    .DW -103
    .DW -109
    .DW -115
    .DW -120
    .DW -126
    .DW -131
    .DW -136
    .DW -142
    .DW -147
    .DW -152
    .DW -157
    .DW -162
    .DW -167
    .DW -171
    .DW -176
    .DW -180
    .DW -185
    .DW -189
    .DW -193
    .DW -197
    .DW -201
    .DW -205
    .DW -208
    .DW -212
    .DW -215
    .DW -219
    .DW -222
    .DW -225
    .DW -228
    .DW -231
    .DW -233
    .DW -236
    .DW -238
    .DW -240
    .DW -242
    .DW -244
    .DW -246
    .DW -247
    .DW -249
    .DW -250
    .DW -251
    .DW -252
    .DW -253
    .DW -254
    .DW -255
    .DW -255
    .DW -256
    .DW -256
    .DW -256
    .DW -255
    .DW -255
    .DW -254
    .DW -253
    .DW -252
    .DW -251
    .DW -250
    .DW -249
    .DW -247
    .DW -246
    .DW -244
    .DW -242
    .DW -240
    .DW -238
    .DW -236
    .DW -233
    .DW -231
    .DW -228
    .DW -225
    .DW -222
    .DW -219
    .DW -215
    .DW -212
    .DW -208
    .DW -205
    .DW -201
    .DW -197
    .DW -193
    .DW -189
    .DW -185
    .DW -180
    .DW -176
    .DW -171
    .DW -167
    .DW -162
    .DW -157
    .DW -152
    .DW -147
    .DW -142
    .DW -136
    .DW -131
    .DW -126
    .DW -120
    .DW -115
    .DW -109
    .DW -103
    .DW -97
    .DW -92
    .DW -86
    .DW -80
    .DW -74
    .DW -68
    .DW -62
    .DW -56
    .DW -49
    .DW -43
    .DW -37
    .DW -31
    .DW -25
    .DW -18
    .DW -12
    .DW -6
.ENDGLOBAL
.GLOBAL
_global_FOREGROUND:
    .DB 1
.ENDGLOBAL
.GLOBAL
_global_WIDTH:
    .DB 255
.ENDGLOBAL
.GLOBAL
_global_HEIGHT:
    .DB 255
.ENDGLOBAL
.GLOBAL
_global_CENTER:
    .DB 128
.ENDGLOBAL
.GLOBAL
_global_FOV:
    .DW 96
.ENDGLOBAL
.GLOBAL
_global_dz:
    .DW 0
.ENDGLOBAL
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 36
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 3
    BLITMODE r1
    MOV r1, r15
    IADD r1, 10
    LDI r2, 65504
    STA r2, r1
    LDI r2, 32
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    LDI r2, 96
    MOV r3, r1
    IADD r3, 4
    STA r2, r3
    LDI r2, 32
    MOV r3, r1
    IADD r3, 6
    STA r2, r3
    LDI r2, 32
    MOV r3, r1
    IADD r3, 8
    STA r2, r3
    LDI r2, 96
    MOV r3, r1
    IADD r3, 10
    STA r2, r3
    LDI r2, 32
    MOV r3, r1
    IADD r3, 12
    STA r2, r3
    LDI r2, 65504
    MOV r3, r1
    IADD r3, 14
    STA r2, r3
    LDI r2, 96
    MOV r3, r1
    IADD r3, 16
    STA r2, r3
    LDI r2, 65504
    MOV r3, r1
    IADD r3, 18
    STA r2, r3
    LDI r2, 65504
    MOV r3, r1
    IADD r3, 20
    STA r2, r3
    LDI r2, 96
    MOV r3, r1
    IADD r3, 22
    STA r2, r3
while_start_L1:
    LDI r1, 1
    ICMP r1, 0
    JEQ while_end_L2
    XOR r1, r1
    CLS r1
    MOV r1, r15
    IADD r1, 10
    XOR r2, r2
    LDI r3, 6
    MUL r2, r3
    ADD r1, r2
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 6
    CALL SYS_MEM_MOVE
    MOV r1, r15
    IADD r1, 6
    MOV r0, r15
    IADD r0, 34
    STA r1, r0
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 6
    CALL _func_draw_point
    MOV r1, r15
    IADD r1, 10
    LDI r2, 6
    ADD r1, r2
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 6
    CALL SYS_MEM_MOVE
    MOV r1, r15
    IADD r1, 6
    MOV r0, r15
    IADD r0, 34
    STA r1, r0
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 6
    CALL _func_draw_point
    MOV r1, r15
    IADD r1, 10
    LDI r2, 12
    ADD r1, r2
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 6
    CALL SYS_MEM_MOVE
    MOV r1, r15
    IADD r1, 6
    MOV r0, r15
    IADD r0, 34
    STA r1, r0
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 6
    CALL _func_draw_point
    MOV r1, r15
    IADD r1, 10
    LDI r2, 18
    ADD r1, r2
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 6
    CALL SYS_MEM_MOVE
    MOV r1, r15
    IADD r1, 6
    MOV r0, r15
    IADD r0, 34
    STA r1, r0
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 6
    CALL _func_draw_point
    VBLNK
    JMP while_start_L1
while_end_L2:
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 36
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_draw_point:
    PUSH r8
    MOV r8, r1
    LDP r1, r8
    LDI r2, 255
    CMP r1, r2
    JLE if_L4
    JMP epilogue_L3
if_L4:
    LDP r1, r8
    ICMP r1, 0
    JGE if_L5
    JMP epilogue_L3
if_L5:
    MOV r2, r8
    IADD r2, 2
    LDP r1, r2
    LDI r2, 255
    CMP r1, r2
    JLE if_L6
    JMP epilogue_L3
if_L6:
    MOV r2, r8
    IADD r2, 2
    LDP r1, r2
    ICMP r1, 0
    JGE if_L7
    JMP epilogue_L3
if_L7:
    MOV r2, r8
    IADD r2, 2
    LDP r1, r2
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    LDP r2, r8
    IAND r2, 255
    OR r1, r2
    LDI r2, 1
    STV r2, r1
epilogue_L3:
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_world_to_screen:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r9, r2
    IADD r2, 4
    LDP r1, r2
    ICMP r1, 0
    JGT if_L9
    LDI r1, 65535
    STA r1, r15
    LDI r1, 65535
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r1, r15
    PUSH r1
    MOV r1, r8
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    JMP epilogue_L8
if_L9:
    LDI r1, 128
    LDP r2, r9
    LDI r3, 96
    MUL r2, r3
    MOV r4, r9
    IADD r4, 4
    LDP r3, r4
    DIV r2, r3
    ADD r1, r2
    STA r1, r15
    LDI r1, 128
    MOV r3, r9
    IADD r3, 2
    LDP r2, r3
    LDI r3, 96
    MUL r2, r3
    MOV r4, r9
    IADD r4, 4
    LDP r3, r4
    DIV r2, r3
    ADD r1, r2
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r1, r15
    PUSH r1
    MOV r1, r8
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
epilogue_L8:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_translate_z:
    PUSH r8
    MOV r8, r1
    MOV r2, r8
    IADD r2, 4
    LDP r1, r2
    LDM r2, _global_dz
    ADD r1, r2
    MOV r2, r8
    IADD r2, 4
    STA r1, r2
epilogue_L10:
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

