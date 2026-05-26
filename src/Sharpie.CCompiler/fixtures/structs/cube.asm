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
_global_edges:
    .DB 0
    .DB 1
    .DB 1
    .DB 2
    .DB 2
    .DB 3
    .DB 3
    .DB 0
    .DB 4
    .DB 5
    .DB 5
    .DB 6
    .DB 6
    .DB 7
    .DB 7
    .DB 4
    .DB 0
    .DB 4
    .DB 1
    .DB 5
    .DB 2
    .DB 6
    .DB 3
    .DB 7
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
    ISUB r6, 22
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
    XOR r8, r8
for_start_L3:
    MOV r1, r8
    LDI r2, 24
    CMP r1, r2
    JGE for_end_L5
    LDI r1, _global_vertices
    LDI r3, _global_edges
    ADD r3, r8
    ALT LDP r2, r3
    LDI r3, 6
    MUL r2, r3
    ADD r1, r2
    MOV r3, r1
    MOV r4, r15
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    LDI r1, _global_vertices
    LDI r3, _global_edges
    MOV r4, r8
    INC r4
    ADD r3, r4
    ALT LDP r2, r3
    LDI r3, 6
    MUL r2, r3
    ADD r1, r2
    MOV r2, r15
    IADD r2, 6
    MOV r3, r1
    MOV r4, r2
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    MOV r1, r15
    ALT LDM r2, _global_angle
    CALL _func_rotate_xz
    MOV r1, r15
    IADD r1, 6
    ALT LDM r2, _global_angle
    CALL _func_rotate_xz
    MOV r2, r15
    IADD r2, 4
    LDP r1, r2
    IADD r1, 96
    MOV r2, r15
    IADD r2, 4
    STA r1, r2
    MOV r2, r15
    IADD r2, 10
    LDP r1, r2
    IADD r1, 96
    MOV r2, r15
    IADD r2, 10
    STA r1, r2
    MOV r1, r15
    IADD r1, 12
    MOV r2, r15
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 16
    MOV r0, r15
    IADD r0, 20
    STA r1, r0
    MOV r2, r15
    IADD r2, 6
    CALL _func_world_to_screen
    MOV r1, r15
    IADD r1, 12
    MOV r2, r15
    IADD r2, 16
    CALL _func_line_bresenham
    ALT VBLNK
for_inc_L4:
    MOV r2, r8
    IADD r2, 2
    MOV r8, r2
    JMP for_start_L3
for_end_L5:
    ALT LDM r2, _global_dAngle
    INC r2
    MOV r1, r2
    ALT STM r2, _global_dAngle
    ICMP r1, 30
    JNE if_L6
    ALT LDM r1, _global_angle
    INC r1
    ALT STM r1, _global_angle
    XOR r1, r1
    ALT STM r1, _global_dAngle
    VBLNK
if_L6:
    ALT LDM r1, _global_angle
    ICMP r1, 192
    JNE if_L7
    XOR r1, r1
    ALT STM r1, _global_angle
if_L7:
    JMP while_start_L1
while_end_L2:
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    IADD r6, 22
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL

.GLOBAL

_func_draw_point:
    PUSH r8
    MOV r8, r1
    LDP r2, r1
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L19
    XOR r1, r1
    JMP rel_end_L20
rel_true_L19:
    LDI r1, 1
rel_end_L20:
    ICMP r1, 0
    JNE logical_true_L16
    LDP r2, r8
    ICMP r2, 0
    JLT rel_true_L21
    XOR r1, r1
    JMP rel_end_L22
rel_true_L21:
    LDI r1, 1
rel_end_L22:
    ICMP r1, 0
    JNE logical_true_L16
    XOR r1, r1
    JMP logical_end_L18
logical_true_L16:
    LDI r1, 1
logical_end_L18:
    ICMP r1, 0
    JNE logical_true_L13
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    LDI r3, 255
    CMP r2, r3
    JGT rel_true_L23
    XOR r1, r1
    JMP rel_end_L24
rel_true_L23:
    LDI r1, 1
rel_end_L24:
    ICMP r1, 0
    JNE logical_true_L13
    XOR r1, r1
    JMP logical_end_L15
logical_true_L13:
    LDI r1, 1
logical_end_L15:
    ICMP r1, 0
    JNE logical_true_L10
    MOV r3, r8
    IADD r3, 2
    LDP r2, r3
    ICMP r2, 0
    JLT rel_true_L25
    XOR r1, r1
    JMP rel_end_L26
rel_true_L25:
    LDI r1, 1
rel_end_L26:
    ICMP r1, 0
    JNE logical_true_L10
    XOR r1, r1
    JMP logical_end_L12
logical_true_L10:
    LDI r1, 1
logical_end_L12:
    ICMP r1, 0
    JEQ if_L9
    JMP epilogue_L8
if_L9:
    MOV r2, r8
    IADD r2, 2
    LDP r1, r2
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    LDP r2, r8
    IAND r2, 255
    OR r1, r2
    LDI r2, 4
    STV r2, r1
epilogue_L8:
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
    ISUB r6, 4
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r9, r2
    IADD r2, 4
    LDP r1, r2
    ICMP r1, 0
    JGT if_L28
    LDI r1, 65535
    STA r1, r15
    LDI r1, 65535
    MOV r2, r6
    IADD r2, 2
    STA r1, r2
    MOV r2, r6
    MOV r3, r8
    LDP r4, r2
    STA r4, r3
    IADD r2, 2
    IADD r3, 2
    LDP r4, r2
    STA r4, r3
    JMP epilogue_L27
if_L28:
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
    MOV r2, r15
    MOV r3, r8
    LDP r4, r2
    STA r4, r3
    IADD r2, 2
    IADD r3, 2
    LDP r4, r2
    STA r4, r3
epilogue_L27:
    MOV r6, r15
    IADD r6, 4
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
epilogue_L29:
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
    MOV r2, r1
    IADD r2, 4
    LDP r1, r2
    ADD r1, r9
    MOV r2, r8
    IADD r2, 4
    STA r1, r2
epilogue_L30:
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
    ISUB r6, 8
    SETSP r6
    MOV r15, r6
    MOV r13, r1
    MOV r14, r2
    XOR r1, r1
    STA r1, r15
    XOR r1, r1
    MOV r2, r6
    IADD r2, 2
    STA r1, r2
    LDP r1, r14
    LDP r2, r13
    SUB r1, r2
    MOV r10, r1
    ICMP r1, 0
    JLE else_L33
    LDI r1, 1
    STA r1, r15
    JMP if_L32
else_L33:
    LDI r1, 65535
    STA r1, r15
    NEG r10
if_L32:
    MOV r2, r14
    IADD r2, 2
    LDP r1, r2
    MOV r3, r13
    IADD r3, 2
    LDP r2, r3
    SUB r1, r2
    MOV r11, r1
    ICMP r1, 0
    JLE else_L35
    LDI r1, 1
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    JMP if_L34
else_L35:
    LDI r1, 65535
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    NEG r11
if_L34:
    LDP r1, r13
    MOV r8, r1
    MOV r2, r13
    IADD r2, 2
    LDP r1, r2
    MOV r9, r1
    CMP r10, r11
    JLE else_L37
    LDI r1, 2
    MOV r2, r11
    MUL r1, r2
    SUB r1, r10
    MOV r12, r1
while_start_L38:
    MOV r1, r8
    LDP r2, r14
    CMP r1, r2
    JEQ while_end_L39
    MOV r2, r8
    ICMP r2, 0
    JGE rel_true_L50
    XOR r1, r1
    JMP rel_end_L51
rel_true_L50:
    LDI r1, 1
rel_end_L51:
    ICMP r1, 0
    JEQ logical_false_L48
    MOV r2, r8
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L52
    XOR r1, r1
    JMP rel_end_L53
rel_true_L52:
    LDI r1, 1
rel_end_L53:
    ICMP r1, 0
    JEQ logical_false_L48
    LDI r1, 1
    JMP logical_end_L49
logical_false_L48:
    XOR r1, r1
logical_end_L49:
    ICMP r1, 0
    JEQ logical_false_L45
    MOV r2, r9
    ICMP r2, 0
    JGE rel_true_L54
    XOR r1, r1
    JMP rel_end_L55
rel_true_L54:
    LDI r1, 1
rel_end_L55:
    ICMP r1, 0
    JEQ logical_false_L45
    LDI r1, 1
    JMP logical_end_L46
logical_false_L45:
    XOR r1, r1
logical_end_L46:
    ICMP r1, 0
    JEQ logical_false_L42
    MOV r2, r9
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L56
    XOR r1, r1
    JMP rel_end_L57
rel_true_L56:
    LDI r1, 1
rel_end_L57:
    ICMP r1, 0
    JEQ logical_false_L42
    LDI r1, 1
    JMP logical_end_L43
logical_false_L42:
    XOR r1, r1
logical_end_L43:
    ICMP r1, 0
    JEQ if_L40
    MOV r1, r9
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    MOV r2, r8
    IAND r2, 255
    OR r1, r2
    LDI r2, 4
    STV r2, r1
if_L40:
    MOV r1, r12
    ICMP r1, 0
    JLT if_L58
    MOV r1, r9
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r9, r1
    MOV r1, r12
    LDI r2, 2
    MOV r3, r10
    MUL r2, r3
    SUB r1, r2
    MOV r12, r1
if_L58:
    MOV r1, r12
    LDI r2, 2
    MOV r3, r11
    MUL r2, r3
    ADD r1, r2
    MOV r12, r1
    MOV r1, r8
    LDP r2, r15
    ADD r1, r2
    MOV r8, r1
    JMP while_start_L38
while_end_L39:
    JMP if_L36
else_L37:
    LDI r1, 2
    MOV r2, r10
    MUL r1, r2
    SUB r1, r11
    MOV r12, r1
while_start_L59:
    MOV r1, r9
    MOV r3, r14
    IADD r3, 2
    LDP r2, r3
    CMP r1, r2
    JEQ while_end_L60
    MOV r2, r8
    ICMP r2, 0
    JGE rel_true_L71
    XOR r1, r1
    JMP rel_end_L72
rel_true_L71:
    LDI r1, 1
rel_end_L72:
    ICMP r1, 0
    JEQ logical_false_L69
    MOV r2, r8
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L73
    XOR r1, r1
    JMP rel_end_L74
rel_true_L73:
    LDI r1, 1
rel_end_L74:
    ICMP r1, 0
    JEQ logical_false_L69
    LDI r1, 1
    JMP logical_end_L70
logical_false_L69:
    XOR r1, r1
logical_end_L70:
    ICMP r1, 0
    JEQ logical_false_L66
    MOV r2, r9
    ICMP r2, 0
    JGE rel_true_L75
    XOR r1, r1
    JMP rel_end_L76
rel_true_L75:
    LDI r1, 1
rel_end_L76:
    ICMP r1, 0
    JEQ logical_false_L66
    LDI r1, 1
    JMP logical_end_L67
logical_false_L66:
    XOR r1, r1
logical_end_L67:
    ICMP r1, 0
    JEQ logical_false_L63
    MOV r2, r9
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L77
    XOR r1, r1
    JMP rel_end_L78
rel_true_L77:
    LDI r1, 1
rel_end_L78:
    ICMP r1, 0
    JEQ logical_false_L63
    LDI r1, 1
    JMP logical_end_L64
logical_false_L63:
    XOR r1, r1
logical_end_L64:
    ICMP r1, 0
    JEQ if_L61
    MOV r1, r9
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    MOV r2, r8
    IAND r2, 255
    OR r1, r2
    LDI r2, 4
    STV r2, r1
if_L61:
    MOV r1, r12
    ICMP r1, 0
    JLT if_L79
    MOV r1, r8
    LDP r2, r15
    ADD r1, r2
    MOV r8, r1
    MOV r1, r12
    LDI r2, 2
    MOV r3, r11
    MUL r2, r3
    SUB r1, r2
    MOV r12, r1
if_L79:
    MOV r1, r12
    LDI r2, 2
    MOV r3, r10
    MUL r2, r3
    ADD r1, r2
    MOV r12, r1
    MOV r1, r9
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r9, r1
    JMP while_start_L59
while_end_L60:
if_L36:
    MOV r2, r8
    ICMP r2, 0
    JGE rel_true_L90
    XOR r1, r1
    JMP rel_end_L91
rel_true_L90:
    LDI r1, 1
rel_end_L91:
    ICMP r1, 0
    JEQ logical_false_L88
    MOV r2, r8
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L92
    XOR r1, r1
    JMP rel_end_L93
rel_true_L92:
    LDI r1, 1
rel_end_L93:
    ICMP r1, 0
    JEQ logical_false_L88
    LDI r1, 1
    JMP logical_end_L89
logical_false_L88:
    XOR r1, r1
logical_end_L89:
    ICMP r1, 0
    JEQ logical_false_L85
    MOV r2, r9
    ICMP r2, 0
    JGE rel_true_L94
    XOR r1, r1
    JMP rel_end_L95
rel_true_L94:
    LDI r1, 1
rel_end_L95:
    ICMP r1, 0
    JEQ logical_false_L85
    LDI r1, 1
    JMP logical_end_L86
logical_false_L85:
    XOR r1, r1
logical_end_L86:
    ICMP r1, 0
    JEQ logical_false_L82
    MOV r2, r9
    LDI r3, 255
    CMP r2, r3
    JLE rel_true_L96
    XOR r1, r1
    JMP rel_end_L97
rel_true_L96:
    LDI r1, 1
rel_end_L97:
    ICMP r1, 0
    JEQ logical_false_L82
    LDI r1, 1
    JMP logical_end_L83
logical_false_L82:
    XOR r1, r1
logical_end_L83:
    ICMP r1, 0
    JEQ if_L80
    MOV r1, r9
    IAND r1, 255
    LDI r2, 8
    SHL r1, r2
    MOV r2, r8
    IAND r2, 255
    OR r1, r2
    LDI r2, 4
    STV r2, r1
if_L80:
epilogue_L31:
    MOV r6, r15
    IADD r6, 8
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

