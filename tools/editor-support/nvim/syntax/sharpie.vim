" Auto-generated Sharpie Syntax
if exists("b:current_syntax") | finish | endif
syn keyword sharpieOpcode NOP MOV LDM LDP LDI STM ADD SUB MUL DIV MOD AND OR XOR SHL SHR CMP ADC INC DEC NOT NEG IADD ISUB IMUL IDIV IMOD IAND IOR IXOR ICMP DINC DDEC JMP JEQ JNE JGT JLT JGE JLE CALL RET PUSH POP OUT_R OUT_B OUT_W RND FLIPR SONG SETCRS DRAW INSTR TAG CLS VBLNK PLAY STOP INPUT TEXT ATTR SWC MUTE COL ALT HALT
syn match sharpieRegister /\<[rR]\([0-9]\|1[0-5]\)\>/
syn match sharpieHex /\$[0-9A-Fa-f]\+/
syn match sharpieNote /#[A-Ga-g][#Bb]\?\d\+/
syn match sharpieComment /;.*/
syn match sharpieLabel /\<[A-Za-z_][A-Za-z0-9_]*:/
syn match sharpieNumber /\(\$\|0x\)[0-9A-Fa-f]\+/
syn match sharpieNumber /0b[01]\+/
syn match sharpieNumber /\<\d\+\>/
syn match sharpieDirective /\.[A-Z]\+/
syn region sharpieString start=/\"/ end=/\"/ 
let b:current_syntax = "sharpie"
hi def link sharpieOpcode Statement
hi def link sharpieRegister Type
hi def link sharpieHex Constant
hi def link sharpieNote Special
hi def link sharpieComment Comment
hi def link sharpieLabel Function
hi def link sharpieNumber Number
hi def link sharpieDirective PreProc
hi def link sharpieString String
