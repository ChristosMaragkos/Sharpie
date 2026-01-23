; SYS_IDX_READ_VAL(start, index, stride)
;
; Origin: $FA2A
;
; Loads a value from an index within a lookup table (LUT) and
; saves it to memory. Also useful for structs.
;
; The CPU calculates (stride × index), adds it to the starting address,
; and reads (stride) consecutive bytes starting from the resulting address.
; Then, the results are saved to work RAM starting at $E805 and ending at $E805 + (stride - 1).
;
; Parameters:
; $E800 - Start: The memory address of the first element of the LUT. 2 bytes.
; $E802 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
; $E804 - Stride: The size of each element in the LUT in bytes. 1 byte.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; All other registers are preserved.
.ORG $FA2A
LutRead:
.SCOPE
    .DEF FirstLutElementParameter $E800
    .DEF IdxParameter $E802
    .DEF StrideParameter $E804
    .DEF OutputAddr $E805

    LDM r0, FirstLutElementParameter
    LDM r1, IdxParameter
    LDM r2, StrideParameter

    MUL r1, r2
    ADD r0, r1

; We don't DEC r2 so we can compare it to 0 (unsigned values only, remember?)

    LDI r3, OutputAddr

    Loop:
        ALT LDP r1, r0 ; Load byte from [r0]
        ALT STR r1, r3

        INC r3
        DEC r2

        ICMP r2, 0
        JGT Loop

        RET
.ENDSCOPE

; SYS_STACKALLOC(addr, byteAmount)
;
; Address: $FA4E
;
; Copies (byteAmount) bytes to the stack, starting at (addr). The bytes are pushed in reverse order,
; so structs are accessed the correct way.
; Use this to temporarily allocate memory on the stack without needing to worry about juggling addresses,
; but be careful if your program stores variables high in work RAM because the CPU will happily overwrite those with stack values.
;
; After saving to the stack, you can POP or ALT POP each value into a register to perform your logic.
;
; Parameters:
; - StartAddress: $E800 (2 bytes)
; - ByteAmount: $E802 (1 byte)
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; The rest are preserved.
Stackalloc:
.SCOPE
    .DEF StartAddressParam $E800
    .DEF ByteAmountParam $E802

    LDM r0, StartAddressParam
    ALT LDM r1, ByteAmountParam

    MOV r2, r0
    ADD r0, r1
    DEC r0 ; We read and write backwards

    POP r3 ; Avoid burying the return address

    Loop:
        ALT LDP r1, r0 ; Load value from [r0]
        ALT PUSH r1

        DEC r0
        CMP r0, r2
        JGE Loop

    PUSH r3
    RET
.ENDSCOPE

; SYS_FRAME_DELAY(frameAmount)
;
; Address: $FA6F
;
; Waits (frameAmount) frames by forcing V-Blank, then returns.
;
; Parameters:
; - FrameAmount: $E800 - The amount of frames to wait for
;
; This subroutine overwrites these registers:
; - R15
FrameDelay:
.SCOPE
    .DEF FrameAmountParam $E800

    LDM r15, FrameAmountParam

    Loop:
        VBLNK
        DEC r15
        ICMP r15, 0
        JNE Loop ; No need to ICMP since DEC updates flags with right operand 1

    RET
.ENDSCOPE
