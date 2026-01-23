# Sharpie BIOS calls

At startup, the Sharpie BIOS loads a few pre-defined subroutines into the reserved ROM space. These aim
to help you with more menial tasks so you can focus on actual game logic.

## SYS_MEM_IDX_READ(start, index, stride)

**Address: $FA2A** 

Loads a value from an index within a lookup table (LUT) and
saves it to memory. Also useful for structs.
The CPU calculates (stride × index), adds it to the starting address,
and reads (stride) consecutive bytes starting from the resulting address.
Then, the results are saved to work RAM starting at $E805 and ending at $E805 + (stride - 1).

### Parameters:
- $E800 - Start: The memory address of the first element of the LUT. 2 bytes.
- $E802 - Index: The zero-based index of the element we want to retrieve. 2 bytes.
- $E804 - Stride: The size of each element in the LUT in bytes. 1 byte.

### Notes:
If stride is 1, an 8-bit write is performed, so only $E805 is overwritten.
If stride ≥ 2, (stride) consecutive bytes are written starting at $E805,
using 16-bit reads and writes.

This subroutine overwrites these registers:
- R0
- R1
- R2
- R3
All other registers are preserved.

## SYS_STACKALLOC(addr, byteAmount)

**Address: $FA4E**

Copies (byteAmount) bytes to the stack, starting at (addr). The bytes are pushed in reverse order,
so structs are accessed the correct way.
Use this to temporarily allocate memory on the stack without needing to worry about juggling addresses,
but be careful if your program stores variables high in work RAM because the CPU will happily overwrite those with stack values.

After saving to the stack, you can POP or ALT POP each value into a register to perform your logic.

### Parameters:
- StartAddress: $E800 (2 bytes)
- ByteAmount: $E802 (1 byte)

### Notes:
This subroutine overwrites these registers:
- R0
- R1
- R2
- R3
The rest are preserved.
