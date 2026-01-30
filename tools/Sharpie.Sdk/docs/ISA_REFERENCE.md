| Opcode | Mnemonic | Arguments | Length | Description | ALT Prefix Function |
| :---: | :---: | :---: | :---: | :--- | :--- |
| `0x00` | **NOP** | `-` | 1 | No operation. |  |
| `0x01` | **MOV** | `R, R` | 2 | Copy value from R2 to R1. |  |
| `0x10` | **LDM** | `R, W` | 4 | Load 16-bit word from address W to R. | Load single byte from address W. |
| `0x11` | **LDP** | `R, R` | 2 | Load word from `[R2]` into R1. | Load single byte from `[R2]`. |
| `0x20n` | **LDI** | `R, W` | 3 | Load immediate word W into Rn. |  |
| `0x30n` | **STM** | `R, W` | 3 | Store word from R to address W. | Store the low byte of R to W. |
| `0x12` | **STP** | `R, R` | 2 | Save word from `[R1]` in the address within R2. | Store the low byte of `[R1]` in the address within R2. |
| `0x13` | **STA** | `R, R` | 2 | Store word from R1 to the address contained in R2. | Store the low byte of R1 at the address contained in R2. |
| `0x40` | **ADD** | `R, R` | 2 | R1 = R1 + R2. Updates Z, N, C, V. |  |
| `0x41` | **SUB** | `R, R` | 2 | R1 = R1 - R2. Updates Z, N, C, V. |  |
| `0x42` | **MUL** | `R, R` | 2 | R1 = R1 * R2. Sets C/V if result > 65535. |  |
| `0x43` | **DIV** | `R, R` | 2 | R1 = R1 / R2. (Div by 0 sets Overflow/Zero). |  |
| `0x44` | **MOD** | `R, R` | 2 | R1 = R1 % R2. |  |
| `0x45` | **AND** | `R, R` | 2 | Bitwise AND. |  |
| `0x46` | **OR** | `R, R` | 2 | Bitwise OR. |  |
| `0x47` | **XOR** | `R, R` | 2 | Bitwise XOR. |  |
| `0x48` | **SHL** | `R, R` | 2 | Logical shift left R1 by (R2 & 0xF). |  |
| `0x49` | **SHR** | `R, R` | 2 | Logical shift right R1 by (R2 & 0xF). |  |
| `0x4A` | **CMP** | `R, R` | 2 | Internal subtraction to set flags. |  |
| `0x4B` | **ADC** | `R, R` | 2 | Add with Carry: R1 + R2 + C. |  |
| `0x50` | **INC** | `R` | 2 | Increment register R by 1. |  |
| `0x51` | **DEC** | `R` | 2 | Decrement register R by 1. |  |
| `0x52` | **NOT** | `R` | 2 | Bitwise NOT (Invert all bits). |  |
| `0x53` | **NEG** | `R` | 2 | Two's complement negation (0 - R). |  |
| `0x60` | **IADD** | `R, B` | 3 | Add immediate byte B to R. | Add byte B to word at `[R]` in memory. |
| `0x61` | **ISUB** | `R, B` | 3 | Subtract immediate byte B from R. | Sub byte B from word at `[R]` in memory. |
| `0x62` | **IMUL** | `R, B` | 3 | Multiply R by immediate byte B. | Mul word at `[R]` by B in memory. |
| `0x63` | **IDIV** | `R, B` | 3 | Divide R by immediate byte B. | Div word at `[R]` by B in memory. |
| `0x64` | **IMOD** | `R, B` | 3 | Modulo R by immediate byte B. | Mod word at `[R]` by B in memory. |
| `0x65` | **IAND** | `R, B` | 3 | Bitwise AND R with byte B. | And word at `[R]` with B in memory. |
| `0x66` | **IOR** | `R, B` | 3 | Bitwise OR R with byte B. | Or word at `[R]` with B in memory. |
| `0x67` | **IXOR** | `R, B` | 3 | Bitwise XOR R with byte B. | Xor word at `[R]` with B in memory. |
| `0x68` | **ICMP** | `R, B` | 3 | Compare R with immediate byte B. | Compare word at `[R]` with byte B in memory. |
| `0x69` | **DINC** | `R` | 2 | Increment word at memory address `[R]`. |  |
| `0x6A` | **DDEC** | `R` | 2 | Decrement word at memory address `[R]`. |  |
| `0x70` | **JMP** | `W` | 3 | Unconditional jump to address W. |  |
| `0x71` | **JEQ** | `W` | 3 | Jump if Zero (equal). |  |
| `0x72` | **JNE** | `W` | 3 | Jump if Not Zero (not equal). |  |
| `0x73` | **JGT** | `W` | 3 | Jump if Greater Than. |  |
| `0x74` | **JLT** | `W` | 3 | Jump if Less Than. |  |
| `0x75` | **JGE** | `W` | 3 | Jump if Greater or Equal. |  |
| `0x76` | **JLE** | `W` | 3 | Jump if Less or Equal. |  |
| `0x77` | **CALL** | `W` | 3 | Push PC+3 to stack and jump to W. | Push PC+3 to stack and jump to the address contained in R1. |
| `0x78` | **RET** | `-` | 1 | Pop address from stack and jump back. |  |
| `0x79` | **PUSH** | `R` | 2 | Push value in R onto stack. | Push the low byte of R onto the stack |
| `0x7A` | **POP** | `R` | 2 | Pop from stack into R. | Pop single byte from stack into R |
| `0x7B` | **OUT_R** | `R` | 2 | Log register R to debug console. |  |
| `0x7C` | **OUT_B** | `B` | 2 | Log immediate byte B to debug console. |  |
| `0x7D` | **OUT_W** | `W` | 3 | Log immediate word W to debug console. |  |
| `0x80n` | **RND** | `R, W` | 3 | Family. Random (0 to W-1) into Rn. |  |
| `0x90` | **FLIPR** | `-` | 1 | Flips between using registers 0-15 and 16-31. |  |
| `0x91` | **CAM** | `R, R` | 2 | Relatively moves camera by the values in (rX,rY). The values in rX and rY are treated as signed. | Sets the camera position to (rX,rY) |
| `0x92` | **GETOAM** | `R` | 2 | Copies the current value of the OAM cursor to rX. |  |
| `0x93` | **SETOAM** | `R` | 2 | Sets the value of the OAM cursor to the value in rX |  |
| `0x94` | **GETSEQ** | `R` | 2 | Copies the current value of the Sequencer cursor to rX. |  |
| `0x95` | **SETSEQ** | `R` | 2 | Sets the value of the Sequencer cursor to the value in rX. Does not start playing. |  |
| `0xA0n` | **SONG** | `R` | 1 | Family. Start music from address in Rn. |  |
| `0xC0` | **SETCRS** | `B, B` | 3 | Set text cursor to (X, Y). | Relative Move cursor by (X, Y). |
| `0xD0n` | **DRAW** | `R, R, R, R` | 3 | Family. Draw sprite: X, Y, ID, Attr. & Type (Low byte -> attr, high byte -> type) |  |
| `0xE0n` | **INSTR** | `R, B, B` | 3 | Family. Define ADSR for instrument Rn. |  |
| `0xC1` | **OAMPOS** | `R, R, R` | 3 | Set R2, R3 to the (X, Y) coordinates of the tile at OAM`[R1]` |  |
| `0xC2` | **SAVE** | `-` | 1 | Alerts the motherboard that a save was requested. |  |
| `0xF0` | **OAMTAG** | `R, R` | 2 | Pack Attribute and Type bytes of OAM`[R1]` into R2. Low Endian. | Set R2 to the Tile ID of OAM`[R1]` |
| `0xF1` | **CLS** | `R` | 2 | Clear screen with color in R and invalidate OAM entries from the current OAM cursor position. | Hard Clear: Wipe screen and reset OAM. |
| `0xF2` | **VBLNK** | `-` | 1 | Yield CPU until next V-Blank. |  |
| `0xF3` | **PLAY** | `R, R, R` | 3 | Play note: Channel, Note, Instrument. Cannot be retriggered by the sequencer until the note is over. |  |
| `0xF4` | **STOP** | `R` | 2 | Stop sound on channel in R. |  |
| `0xF5` | **INPUT** | `R, R` | 2 | Read Controller `[R1]` into R2. |  |
| `0xF7` | **TEXT** | `B` | 2 | Draw ASCII char B at cursor. | Print Register: Interpret B as register index and draw its value. |
| `0xF8` | **ATTR** | `B` | 2 | Set global text color/attributes. |  |
| `0xF9` | **SWC** | `R, R` | 2 | Swap Palette: Active`[R1]` = Master`[R2]`. |  |
| `0xFA` | **BANK** | `R` | 2 | Switch current bank to R1. |  |
| `0xFC` | **MUTE** | `-` | 1 | Toggle music sequencer. | Hard Silence: Stop all sound output. |
| `0xFD` | **COL** | `R, R` | 2 | Check Collision for OAM`[R1]`, store in R2. |  |
| `0xFE` | **ALT** | `-` | 1 | Prefix. Modifies next opcode logic. |  |
| `0xFF` | **HALT** | `-` | 1 | Terminate CPU (hard-stop). |  |
