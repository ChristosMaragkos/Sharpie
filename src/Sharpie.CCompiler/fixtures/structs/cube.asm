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
    .DB 128
.ENDGLOBAL
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
_global_dz:
    .DW 0
.ENDGLOBAL
.GLOBAL
_global_ddz:
    .DB 0
.ENDGLOBAL
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 3
    BLITMODE r1
while_start_L1:
    LDI r1, 1
    ICMP r1, 0
    JEQ while_end_L2
    XOR r1, r1
    CLS r1
    MOV r1, r15
    LDI r2, 40
    STA r2, r1
    LDI r2, 40
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    MOV r2, r15
    IADD r2, 2
    LDP r1, r2
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    LDP r2, r15
    IAND r2, 255
    OR r1, r2
    LDI r2, 1
    STV r2, r1
    VBLNK
    JMP while_start_L1
while_end_L2:
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_draw_point:
    PUSH r8
    MOV r8, r1
    LDP r2, r8
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L14
    XOR r1, r1
    JMP rel_end_L15
rel_true_L14:
    LDI r1, 1
rel_end_L15:
    ICMP r1, 0
    JNE logical_true_L11
    LDP r2, r8
    ICMP r2, 0
    JLT rel_true_L16
    XOR r1, r1
    JMP rel_end_L17
rel_true_L16:
    LDI r1, 1
rel_end_L17:
    ICMP r1, 0
    JNE logical_true_L11
    XOR r1, r1
    JMP logical_end_L13
logical_true_L11:
    LDI r1, 1
logical_end_L13:
    ICMP r1, 0
    JNE logical_true_L8
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L18
    XOR r1, r1
    JMP rel_end_L19
rel_true_L18:
    LDI r1, 1
rel_end_L19:
    ICMP r1, 0
    JNE logical_true_L8
    XOR r1, r1
    JMP logical_end_L10
logical_true_L8:
    LDI r1, 1
logical_end_L10:
    ICMP r1, 0
    JNE logical_true_L5
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    ICMP r2, 0
    JLT rel_true_L20
    XOR r1, r1
    JMP rel_end_L21
rel_true_L20:
    LDI r1, 1
rel_end_L21:
    ICMP r1, 0
    JNE logical_true_L5
    XOR r1, r1
    JMP logical_end_L7
logical_true_L5:
    LDI r1, 1
logical_end_L7:
    ICMP r1, 0
    JEQ if_L4
    JMP epilogue_L3
if_L4:
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
    PUSH r10
    PUSH r11
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r9, r2
    LDP r1, r2
    LDI r2, 128
    MUL r1, r2
    MOV r3, r9
    IADD r3, 4
    LDP r2, r3
    DIV r1, r2
    LDI r2, 128
    ADD r1, r2
    MOV r10, r1
    MOV r2, r9
    IADD r2, 2
    LDP r1, r2
    LDI r2, 128
    MUL r1, r2
    MOV r3, r9
    IADD r3, 4
    LDP r2, r3
    DIV r1, r2
    LDI r2, 128
    ADD r1, r2
    MOV r11, r1
    MOV r1, r15
    MOV r2, r10
    STA r2, r1
    MOV r2, r11
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    MOV r1, r15
    PUSH r1
    MOV r1, r8
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
epilogue_L22:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r11
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

