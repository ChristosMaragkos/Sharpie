.ORG $FA2A
; SYS_IDX_READ_VAL(start, index, stride)
;
; Address: $FA2A
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
LutRead:
.SCOPE
    .DEF FirstLutElementParameter $E800
    .DEF IdxParameter $E802
    .DEF StrideParameter $E804
    .DEF OutputAddr $E805

    LDM r2, StrideParameter
    ICMP r2, 0
    JEQ Return

    LDM r0, FirstLutElementParameter
    LDM r1, IdxParameter

    MUL r1, r2
    ADD r0, r1

; We don't DEC r2 so we can compare it to 0 (unsigned values only, remember?)

    LDI r3, OutputAddr

    Loop:
        ALT LDP r1, r0 ; Load byte from [r0]
        ALT STA r1, r3

        INC r3
        DEC r2

        ICMP r2, 0
        JGT Loop

    Return:
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

    ALT LDM r1, ByteAmountParam
    ICMP r1, 0
    JEQ Return

    LDM r0, StartAddressParam

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
Return:
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
        JGE Loop ; No need to ICMP since DEC updates flags with right operand 1

    RET
.ENDSCOPE

; SYS_IDX_WRITE_VAL(start, index, stride)
;
; Address: $FA7D
;
; Writes a value from $E805 onwards to a specific index of a LUT. Also useful for structs.
;
; The CPU calculates (stride × index), adds it to the starting address,
; and reads (stride) consecutive bytes starting from $E805.
; Then, the results are saved to the LUT starting at the calculated address.
;
; Parameters:
; $E800 - Start: The memory address of the first element of the LUT. 2 bytes.
; $E802 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
; $E804 - Size: The size of each element in the LUT in bytes. 1 byte.
; $E805 - $E805 + (Size - 1): The element to write to the LUT. (Size) bytes.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; All other registers are preserved.
LutWrite:
.SCOPE
    .DEF FirstAddrParam $E800
    .DEF IndexParam $E802
    .DEF SizeParam $E804
    .DEF FirstBytePtr $E805

    LDM r2, SizeParam
    ICMP r2, 0
    JEQ Return ; No cycles wasted

    LDM r0, FirstAddrParam
    LDM r1, IndexParam

    MUL r1, r2
    ADD r0, r1 ; R0 now holds the first index we're writing to

    LDI r3, FirstBytePtr

    Loop:
        ALT LDP r1, r3
        ALT STP r1, r0
        INC r3
        INC r0

        DEC r2
        JNE Loop

    Return:
        RET
.ENDSCOPE

; SYS_IDX_READ_REF
;
; Address: $FAA6
;
; Calculates a pointer (the address) to a value within a lookup table (LUT) and
; saves it to memory. Similar to SYS_IDX_READ_VAL but with reference type semantics.
;
; The CPU calculates (stride × index) and adds it to the starting address of the LUT.
; Then, the memory address is saved to work RAM, overwriting $E805-$E806
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
; All other registers are preserved.
LutGetPtr:
.SCOPE
    .DEF LutPtrParameter $E800
    .DEF IdxParameter $E802
    .DEF StrideParameter $E804
    .DEF OutputAddr $E805

    LDM r0, LutPtrParameter 
    LDM r1, IdxParameter
    LDM r2, StrideParameter

    MUL r1, r2
    ADD r0, r1

    STM r0, $E805
    RET
.ENDSCOPE

; SYS_MEM_COPY
;
; Address: $FAC3
;
; Copies (byteAmount) bytes from the starting address to the end address.
; This overwrites everything from (end) to (end + byteAmount - 1).
;
; Parameters:
; $E800 - Copy start: The address of the first byte to copy.
; $E802 - Paste start: The address to start copying to.
; $E804 - Byte amount: The amount of bytes to copy.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; All other registers are preserved.
MemCopy:
.SCOPE
    .DEF CopyStartPtr $E800
    .DEF PasteStartPtr $E802
    .DEF AmountParam $E804

    LDM r3, AmountParam
    ICMP r3, 0
    JEQ Return

    LDM r0, CopyStartPtr
    LDM r1, PasteStartPtr

    Loop:
        STP r0, r1
        INC r0
        INC r1

        DEC r3
        JNE Loop

    Return:
        RET
.ENDSCOPE
