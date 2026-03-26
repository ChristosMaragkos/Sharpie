## `SYS_IDX_READ_VAL(start, index, stride)`
**Address:** `$FA2A`

Loads a value from an index within a lookup table (LUT) and saves it to memory. Also useful for structs. The CPU calculates (stride × index), adds it to the starting address, and reads (stride) consecutive bytes starting from the resulting address. Then, the results are saved to work RAM starting at $E805 and ending at $E805 + (stride - 1).

### Parameters
- `$E800`: Start: The memory address of the first element of the LUT. 2 bytes.
- `$E802`: Index: The zero-based index of the element we want to retrieve. 2 bytes.
- `$E804`: Stride: The size of each element in the LUT in bytes. 1 byte.

### Clobbered Registers
- `R0`
- `R1`
- `R2`
- `R3`

## `SYS_STACKALLOC(addr, byteAmount)`
**Address:** `$FA4E`

Copies (byteAmount) bytes to the stack, starting at (addr). The bytes are pushed in reverse order, so structs are accessed the correct way. Use this to temporarily allocate memory on the stack without needing to worry about juggling addresses, but be careful if your program stores variables high in work RAM because the CPU will happily overwrite those with stack values. After saving to the stack, you can POP or ALT POP each value into a register to perform your logic.

### Clobbered Registers
- `R0`
- `R1`
- `R2`
- `R3`

## `SYS_FRAME_DELAY(frameAmount)`
**Address:** `$FA6F`

Waits (frameAmount) frames by forcing V-Blank, then returns.

### Clobbered Registers
- `R15`

## `SYS_IDX_WRITE_VAL(start, index, stride)`
**Address:** `$FA7D`

Writes a value from $E805 onwards to a specific index of a LUT. Also useful for structs. The CPU calculates (stride × index), adds it to the starting address, and reads (stride) consecutive bytes starting from $E805. Then, the results are saved to the LUT starting at the calculated address.

### Parameters
- `$E800`: Start: The memory address of the first element of the LUT. 2 bytes.
- `$E802`: Index: The zero-based index of the element we want to retrieve. 2 bytes.
- `$E804`: Size: The size of each element in the LUT in bytes. 1 byte.
- `$E805`: $E805 + (Size - 1): The element to write to the LUT. (Size) bytes.

### Clobbered Registers
- `R0`
- `R1`
- `R2`
- `R3`

## `SYS_IDX_READ_REF`
**Address:** `$FAA6`

Calculates a pointer (the address) to a value within a lookup table (LUT) and saves it to memory. Similar to SYS_IDX_READ_VAL but with reference type semantics. The CPU calculates (stride × index) and adds it to the starting address of the LUT. Then, the memory address is saved to work RAM, overwriting $E805-$E806

### Parameters
- `$E800`: Start: The memory address of the first element of the LUT. 2 bytes.
- `$E802`: Index: The zero-based index of the element we want to retrieve. 2 bytes.
- `$E804`: Stride: The size of each element in the LUT in bytes. 1 byte.

### Clobbered Registers
- `R0`
- `R1`
- `R2`

## `SYS_MEM_COPY`
**Address:** `$FAC3`

Copies (byteAmount) bytes from the starting address to the end address. This overwrites everything from (end) to (end + byteAmount - 1).

### Parameters
- `$E800`: Copy start: The address of the first byte to copy.
- `$E802`: Paste start: The address to start copying to.
- `$E804`: Byte amount: The amount of bytes to copy.

### Clobbered Registers
- `R0`
- `R1`
- `R2`
- `R3`

## `SYS_PAL_RESET`
**Address:** `$FAE2`

Resets the palette to its default (color 0 points to color 0, color 1 to color 1, and so on.)

