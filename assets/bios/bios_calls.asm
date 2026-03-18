; SYSCALLS
; Licensed under the LGPL-2.1 license.
; This file contains subroutines available to all Sharpie cartridges at runtime. They may be called using their assigned name,
; such as CALL SYS_IDX_READ_VAL.
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
;
; Parameters:
; R1 - Start: The memory address of the first element of the LUT. 2 bytes.
; R2 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
; R3 - Stride: The size of each element in the LUT in bytes. 1 byte.
; R4 - Destination: Where to copy the resulting element
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; - R4
; All other registers are preserved.
LutRead:
.SCOPE
    ICMP r3, 0
    JEQ Return

    MUL r2, r3
    ADD r1, r2

    Loop:
        ALT STP r1, r4
        INC r1
        INC r4

        DEC r3
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
; R1 - The (first) memory address of the block to copy
; R2 - The amount of bytes to copy
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; - R4
; The rest are preserved.
Stackalloc:
.SCOPE
    ICMP r2, 0
    JEQ NoAlloc

    MOV r3, r1
    ADD r1, r2
    DEC r1 ; We read and write backwards

    POP r4 ; Avoid burying the return address

    Loop:
        ALT LDP r0, r1 ; Load value from [r0]
        ALT PUSH r0

        DEC r1
        CMP r1, r3
        JGE Loop

    PUSH r4

    Return:
        GETSP r0
        IADD r0, 2
        RET
    NoAlloc:
        GETSP r0
        RET
.ENDSCOPE

; SYS_FRAME_DELAY(frameAmount)
;
; Address: $FA6F
;
; Waits (frameAmount) frames by forcing V-Blank, then returns.
;
; Parameters:
; - FrameAmount: R1 - The amount of frames to wait for
;
; This subroutine overwrites these registers:
; - R15
FrameDelay:
.SCOPE
    Loop:
        VBLNK
        DEC r1
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
; R1 - Start: The memory address of the first element of the LUT. 2 bytes.
; R2 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
; R3 - Size: The size of each element in the LUT in bytes. 1 byte.
; R4 - The (first) memory address holding the element to copy.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; - R4
; All other registers are preserved.
LutWrite:
.SCOPE
    ICMP r3, 0
    JEQ Return

    MUL r2, r3
    ADD r1, r2

    Loop:
        ALT STP r4, r1
        INC r4
        INC r1

        DEC r3
        JGT Loop

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
;
; Parameters:
; R1 - Start: The memory address of the first element of the LUT. 2 bytes.
; R2 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
; R3 - Stride: The size of each element in the LUT in bytes. 1 byte.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; All other registers are preserved.
LutGetPtr:
.SCOPE
    MUL r2, r3
    ADD r1, r2

    MOV r0, r1
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
; R1 - Paste start: The address of the first byte to copy.
; R2 - Copy start: The address to start copying to.
; R3 - Byte amount: The amount of bytes to copy.
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
; - R3
; All other registers are preserved.
MemCopy:
.SCOPE
    ICMP r3, 0
    JEQ Return

    CMP r2, r1
    JEQ Return

    Loop:
        ALT STP r2, r1
        INC r2
        INC r1

        DEC r3
        JNE Loop

    Return:
        RET
.ENDSCOPE

; SYS_PAL_RESET
;
; Address: $FAE2
;
; Resets the palette to its default (color 0 points to color 0, color 1 to color 1, and so on.)
;
; Parameters:
; None.
;
; This subroutine overwrites these registers:
; None.
ResetPalette:
.SCOPE
    PUSH r4
    LDI r4, 0

    Loop:
        SWC r4, r4
        INC r4
        ICMP r4, 32
        JLT Loop

    POP r4
    RET
.ENDSCOPE

; SYS_ALLOC_STACKFRAME
;
; Address: 0xFAF4
;
; Parameters:
; byte amount (expected in r1)
;
; This subroutine simply allocates N bytes in the stack. It is only meant to be used by C compilers to streamline creating a stack frame.
; After completing the allocation, r0 contains the final address of the stack pointer, effectively returning a pointer to the allocated space.
; If you do use this in assembly, note that the space allocated is left uninitialized (and, being stack space, it contains garbgage).
;
; This subroutine overwrites these registers:
; - R0
; - R1
; - R2
Alloca:
.SCOPE
    ICMP r1, 0
    JEQ Return

    POP r2

    GETSP r0
    SUB r0, r1 ; downwards growing stack
    SETSP r0
 
    PUSH r2

    Return:
        GETSP r0
        IADD r0, 2
        RET

.ENDSCOPE

; SYS_FREE_STACKFRAME
;
; Address:
;
; Parameters:
; byte amount (expected in r1)
;
; The opposite to SYS_ALLOC_STACKFRAME. This subroutine simply frees N bytes from the stack and returns nothing.
;
; This subroutine overwrites these registers:
; - R1
; - R2
FreeFrame:
.SCOPE
    ICMP r1, 0
    JEQ Return

    POP r2

    GETSP r3
    ADD r3, r1
    SETSP r3

    PUSH r2
    Return:
        RET
.ENDSCOPE

; SYS_MEM_SET
; Sets (R3) bytes starting at address (R1) to the value (R2).
MemSet:
.SCOPE
    ICMP r3, 0
    JEQ Return

    Loop:
        ALT STA r2, r1
        INC r1

        DEC r3
        JGT Loop

    Return:
        RET
.ENDSCOPE

; SYS_MEM_CMP
; Compares (R3) bytes in the memory regions starting at (R1) and (R2).
; Returns 0 if they match byte-for-byte, or the difference of the first non-matching bytes,
; which may be negative or positive.
MemCmp:
.SCOPE
    LDI r0, 0
    ICMP r3, 0
    JEQ Return

    Loop:
        ALT LDP r4, r1
        ALT LDP r5, r2

        CMP r4, r5
        JNE Diff

        INC r1
        INC r2

        DEC r3
        JGT Loop

        JMP Return

    Diff:
        MOV r0, r4
        SUB r0, r5

    Return:
        RET
.ENDSCOPE

; SYS_PRINT
; Prints the null(zero)-terminated string starting at (R1) beginning in the grid coordinates at (R2, R3).
Print:
.SCOPE
    CRSPOS r2, r3
    Loop:
        ALT LDP r4, r1
        ICMP r4, 0
        JEQ Return
        PRNT r4
        JMP Loop

    Return:
        RET
.ENDSCOPE

; SYS_MEM_MOVE
; Safely copies the region beginning in R2 into the region beginning in R1 with a size of R3 bytes.
MemMove:
.SCOPE
    CMP r1, r2
    JEQ Return
    JGT Backward

    Forward:
        CALL MemCopy
        RET

    Backward:
        MOV r4, r1
        ADD r1, r3
        ADD r2, r3

        DEC r2 ; zero-based index
        DEC r3

        Loop:
            ALT STP r2, r1

            DEC r1
            DEC r2

            CMP r1, r4
            JGE Loop

    Return:
        RET
.ENDSCOPE
