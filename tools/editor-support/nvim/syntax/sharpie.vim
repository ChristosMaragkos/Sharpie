" Auto-generated Sharpie Syntax
if exists("b:current_syntax") | finish | endif
syn keyword sharpieOpcode NOP nop MOV mov LDM ldm LDP ldp LDI ldi STM stm STP stp STA sta ADD add SUB sub MUL mul DIV div MOD mod AND and OR or XOR xor SHL shl SHR shr CMP cmp ADC adc INC inc DEC dec NOT not NEG neg IADD iadd ISUB isub IMUL imul IDIV idiv IMOD imod IAND iand IOR ior IXOR ixor ICMP icmp DINC dinc DDEC ddec JMP jmp JEQ jeq JNE jne JGT jgt JLT jlt JGE jge JLE jle CALL call RET ret PUSH push POP pop OUT_R out_r OUT_B out_b OUT_W out_w RND rnd FLIPR flipr CAM cam GETOAM getoam SETOAM setoam GETSEQ getseq SETSEQ setseq SONG song SETCRS setcrs DRAW draw INSTR instr OAMPOS oampos OAMTAG oamtag CLS cls VBLNK vblnk PLAY play STOP stop INPUT input TEXT text ATTR attr SWC swc BANK bank MUTE mute COL col ALT alt HALT halt
syn match sharpieRegister /\<[rR]\([0-9]\|1[0-5]\)\>/
syn match sharpieHex /\$[0-9A-Fa-f]\+/
syn match sharpieNote /#[A-Ga-g][#Bb]\?\d\+/
syn match sharpieComment /;.*/
syn match sharpieLabel /\<[A-Za-z_][A-Za-z0-9_]*:/
syn match sharpieNumber /\(\$\|0x\)[0-9A-Fa-f]\+/
syn match sharpieNumber /0b[01]\+/
syn match sharpieNumber /\<\d\+\>/
syn match sharpieDirective /\.[A-Za-z_]\+/
syn match sharpieEnumRef /\<[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\>/
syn region sharpieString start=/\"/ end=/\"/ 
let b:current_syntax = "sharpie"
hi def link sharpieOpcode Statement
hi def link sharpieRegister Type
hi def link sharpieHex Constant
hi def link sharpieNote Special
hi def link sharpieComment Comment
hi def link sharpieLabel Function
hi def link sharpieEnumRef Special
hi def link sharpieNumber Number
hi def link sharpieDirective PreProc
hi def link sharpieString String
