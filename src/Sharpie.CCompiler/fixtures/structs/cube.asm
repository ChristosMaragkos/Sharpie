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
_global_vertices:
    .DW -32
    .DW 32
    .DW 32
    .DW 32
    .DW 32
    .DW 32
    .DW 32
    .DW -32
    .DW 32
    .DW -32
    .DW -32
    .DW 32
    .DW -32
    .DW 32
    .DW -32
    .DW 32
    .DW 32
    .DW -32
    .DW 32
    .DW -32
    .DW -32
    .DW -32
    .DW -32
    .DW -32
.ENDGLOBAL
.GLOBAL
_global_angle:
    .DB 0
.ENDGLOBAL
.GLOBAL
_global_dAngle:
    .DB 0
.ENDGLOBAL
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 22
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
    XOR r1, r1
    MOV r8, r1
for_start_L3:
    MOV r1, r8
    LDI r2, 8
    CMP r1, r2
    JGE for_end_L5
    LDI r1, _global_vertices
    MOV r2, r8
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
    ALT LDM r2, _global_angle
    CALL _func_rotate_xz
    MOV r1, r15
    LDI r2, 96
    CALL _func_translate_z
    MOV r1, r15
    IADD r1, 6
    MOV r0, r15
    IADD r0, 20
    STA r1, r0
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 6
    CALL _func_draw_point
for_inc_L4:
    INC r8
    JMP for_start_L3
for_end_L5:
    ALT LDM r2, _global_dAngle
    INC r2
    MOV r1, r2
    ALT STM r2, _global_dAngle
    ICMP r1, 5
    JNE if_L6
    ALT LDM r1, _global_angle
    INC r1
    ALT STM r1, _global_angle
    XOR r1, r1
    ALT STM r1, _global_dAngle
if_L6:
    VBLNK
    JMP while_start_L1
while_end_L2:
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 22
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_draw_point:
    PUSH r8
    MOV r8, r1
    LDP r2, r8
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L18
    XOR r1, r1
    JMP rel_end_L19
rel_true_L18:
    LDI r1, 1
rel_end_L19:
    ICMP r1, 0
    JNE logical_true_L15
    LDP r2, r8
    ICMP r2, 0
    JLT rel_true_L20
    XOR r1, r1
    JMP rel_end_L21
rel_true_L20:
    LDI r1, 1
rel_end_L21:
    ICMP r1, 0
    JNE logical_true_L15
    XOR r1, r1
    JMP logical_end_L17
logical_true_L15:
    LDI r1, 1
logical_end_L17:
    ICMP r1, 0
    JNE logical_true_L12
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L22
    XOR r1, r1
    JMP rel_end_L23
rel_true_L22:
    LDI r1, 1
rel_end_L23:
    ICMP r1, 0
    JNE logical_true_L12
    XOR r1, r1
    JMP logical_end_L14
logical_true_L12:
    LDI r1, 1
logical_end_L14:
    ICMP r1, 0
    JNE logical_true_L9
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    ICMP r2, 0
    JLT rel_true_L24
    XOR r1, r1
    JMP rel_end_L25
rel_true_L24:
    LDI r1, 1
rel_end_L25:
    ICMP r1, 0
    JNE logical_true_L9
    XOR r1, r1
    JMP logical_end_L11
logical_true_L9:
    LDI r1, 1
logical_end_L11:
    ICMP r1, 0
    JEQ if_L8
    JMP epilogue_L7
if_L8:
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
epilogue_L7:
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
    JGT if_L27
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
    JMP epilogue_L26
if_L27:
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
epilogue_L26:
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
_func_rotate_xz:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r12
    PUSH r13
    MOV r8, r1
    MOV r9, r2
    LDI r2, _global_sin_table
    MOV r3, r9
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    LDP r1, r2
    MOV r10, r1
    LDI r2, _global_sin_table
    MOV r3, r9
    IADD r3, 64
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    LDP r1, r2
    MOV r11, r1
    LDP r1, r8
    MOV r12, r1
    MOV r2, r8
    IADD r2, 4
    LDP r1, r2
    MOV r13, r1
    MOV r1, r12
    MOV r2, r11
    MUL r1, r2
    MOV r2, r13
    MOV r3, r10
    MUL r2, r3
    SUB r1, r2
    LDI r2, 256
    DIV r1, r2
    STA r1, r8
    MOV r1, r12
    MOV r2, r10
    MUL r1, r2
    MOV r2, r13
    MOV r3, r11
    MUL r2, r3
    ADD r1, r2
    LDI r2, 256
    DIV r1, r2
    MOV r2, r8
    IADD r2, 4
    STA r1, r2
epilogue_L28:
    POP r13
    POP r12
    POP r11
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_translate_z:
    PUSH r8
    PUSH r9
    MOV r8, r1
    MOV r9, r2
    MOV r2, r8
    IADD r2, 4
    LDP r1, r2
    MOV r2, r9
    ADD r1, r2
    MOV r2, r8
    IADD r2, 4
    STA r1, r2
epilogue_L29:
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_line_bresenham:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r12
    PUSH r13
    PUSH r14
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 20
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r13, r1
    MOV r14, r2
    XOR r1, r1
    MOV r2, r15
    IADD r2, 4
    STA r1, r2
    XOR r1, r1
    MOV r2, r15
    IADD r2, 6
    STA r1, r2
    LDP r1, r14
    LDP r2, r13
    SUB r1, r2
    MOV r8, r1
    ICMP r1, 0
    JLE else_L32
    LDI r1, 1
    MOV r2, r15
    IADD r2, 4
    STA r1, r2
    JMP if_L31
else_L32:
    LDI r1, 65535
    MOV r2, r15
    IADD r2, 4
    STA r1, r2
    NEG r8
if_L31:
    MOV r2, r14
    IADD r2, 2
    LDP r1, r2
    MOV r3, r13
    IADD r3, 2
    LDP r2, r3
    SUB r1, r2
    MOV r9, r1
    ICMP r1, 0
    JLE else_L34
    LDI r1, 1
    MOV r2, r15
    IADD r2, 6
    STA r1, r2
    JMP if_L33
else_L34:
    LDI r1, 65535
    MOV r2, r15
    IADD r2, 6
    STA r1, r2
    NEG r9
if_L33:
    LDP r1, r13
    MOV r11, r1
    MOV r2, r13
    IADD r2, 2
    LDP r1, r2
    MOV r12, r1
    CMP r8, r9
    JLE else_L36
    LDI r1, 2
    MOV r2, r9
    MUL r1, r2
    MOV r2, r8
    SUB r1, r2
    MOV r10, r1
while_start_L37:
    MOV r1, r11
    LDP r2, r14
    CMP r1, r2
    JEQ while_end_L38
    MOV r2, r15
    IADD r2, 8
    MOV r3, r11
    STA r3, r2
    MOV r3, r12
    MOV r4, r2
    IADD r4, 2
    STA r3, r4
    MOV r1, r15
    IADD r1, 8
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    MOV r1, r15
    CALL _func_draw_point
    MOV r1, r10
    ICMP r1, 0
    JLT if_L40
    MOV r1, r12
    MOV r3, r15
    IADD r3, 6
    LDP r2, r3
    ADD r1, r2
    MOV r12, r1
    MOV r1, r10
    LDI r2, 2
    MOV r3, r8
    MUL r2, r3
    SUB r1, r2
    MOV r10, r1
if_L40:
    MOV r1, r10
    LDI r2, 2
    MOV r3, r9
    MUL r2, r3
    ADD r1, r2
    MOV r10, r1
    MOV r1, r11
    MOV r3, r15
    IADD r3, 4
    LDP r2, r3
    ADD r1, r2
    MOV r11, r1
    JMP while_start_L37
while_end_L38:
    JMP if_L35
else_L36:
    LDI r1, 2
    MOV r2, r8
    MUL r1, r2
    MOV r2, r9
    SUB r1, r2
    MOV r10, r1
while_start_L41:
    MOV r1, r12
    MOV r3, r14
    IADD r3, 2
    LDP r2, r3
    CMP r1, r2
    JEQ while_end_L42
    MOV r2, r15
    IADD r2, 12
    MOV r3, r11
    STA r3, r2
    MOV r3, r12
    MOV r4, r2
    IADD r4, 2
    STA r3, r4
    MOV r1, r15
    IADD r1, 12
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    MOV r1, r15
    CALL _func_draw_point
    MOV r1, r10
    ICMP r1, 0
    JLT if_L44
    MOV r1, r11
    MOV r3, r15
    IADD r3, 4
    LDP r2, r3
    ADD r1, r2
    MOV r11, r1
    MOV r1, r10
    LDI r2, 2
    MOV r3, r9
    MUL r2, r3
    SUB r1, r2
    MOV r10, r1
if_L44:
    MOV r1, r10
    LDI r2, 2
    MOV r3, r8
    MUL r2, r3
    ADD r1, r2
    MOV r10, r1
    MOV r1, r12
    MOV r3, r15
    IADD r3, 6
    LDP r2, r3
    ADD r1, r2
    MOV r12, r1
    JMP while_start_L41
while_end_L42:
if_L35:
    MOV r2, r15
    IADD r2, 16
    MOV r3, r11
    STA r3, r2
    MOV r3, r12
    MOV r4, r2
    IADD r4, 2
    STA r3, r4
    MOV r1, r15
    IADD r1, 16
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    MOV r1, r15
    CALL _func_draw_point
epilogue_L30:
    MOV r6, r15
    LDI r7, 20
    ADD r6, r7
    SETSP r6
    POP r15
    POP r14
    POP r13
    POP r12
    POP r11
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

