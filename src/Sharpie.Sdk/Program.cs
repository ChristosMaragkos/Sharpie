using Sharpie.Sdk.Asm;

var assembler = new Assembler();

// assembler.LoadFile("src/Sharpie.Sdk/test.asm");
assembler.ReadRawAssembly(
    @"
.STR 10, 10, ""HELLO WORLD""
SWC 1 10 ; Swap color
.DEF ADDR 30
.STR ADDR ADDR ""NO""
"
);
