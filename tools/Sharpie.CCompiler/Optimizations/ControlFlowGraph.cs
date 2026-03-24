namespace Sharpie.CCompiler.Optimizations;

public class BasicBlock
{
    public string Name { get; set; } = "";
    public List<Instruction> Instructions { get; } = [];

    // Graph edges
    public List<BasicBlock> Successors { get; } = [];
    public List<BasicBlock> Predecessors { get; } = [];

    public override string ToString() => Name;
}

public class ControlFlowGraph
{
    public List<BasicBlock> Blocks { get; } = [];

    public static ControlFlowGraph Build(List<Instruction> instructions)
    {
        var cfg = new ControlFlowGraph();
        if (instructions.Count == 0)
            return cfg;

        var currentBlock = new BasicBlock { Name = "entry" };

        for (var i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];

            if (inst.IsLabel)
            {
                var labelName = inst.OriginalText.TrimEnd(':');
                if (currentBlock.Instructions.Count > 0)
                {
                    currentBlock = new BasicBlock { Name = labelName };
                    cfg.Blocks.Add(currentBlock);
                }
                else
                {
                    currentBlock.Name = labelName;
                }
            }
            else
            {
                currentBlock.Instructions.Add(inst);

                var isBranch = inst.Mnemonic.StartsWith('J') || inst.Mnemonic is "RET" or "HALT";

                if (isBranch && i < instructions.Count - 1 && !instructions[i + 1].IsLabel)
                {
                    currentBlock = new BasicBlock { Name = $"block_fallthrough_{i}" };
                    cfg.Blocks.Add(currentBlock);
                }
            }
        }

        var blockMap = cfg
            .Blocks.Where(b => b.Instructions.Count > 0 && b.Instructions[0].IsLabel)
            .ToDictionary(b => b.Name, b => b);

        for (int i = 0; i < cfg.Blocks.Count; i++)
        {
            var block = cfg.Blocks[i];
            if (block.Instructions.Count == 0)
                continue;

            var lastInst = block.Instructions.Last();

            if (lastInst.Mnemonic.StartsWith('J'))
            {
                if (blockMap.TryGetValue(lastInst.Arg1, out var targetBlock))
                {
                    block.Successors.Add(targetBlock);
                    targetBlock.Predecessors.Add(block);
                }

                // conditional jump -> two paths (target & fallthrough)
                if (i + 1 < cfg.Blocks.Count && lastInst.Mnemonic is not "JMP")
                {
                    block.Successors.Add(cfg.Blocks[i + 1]);
                    cfg.Blocks[i + 1].Predecessors.Add(block);
                }
            }
            else if (lastInst.Mnemonic is "RET" or "HALT")
            {
                // do nothing
            }
            else if (i + 1 < cfg.Blocks.Count)
            {
                block.Successors.Add(cfg.Blocks[i + 1]);
                cfg.Blocks[i + 1].Predecessors.Add(block);
            }
        }

        return cfg;
    }

    public List<Instruction> Flatten()
    {
        var list = new List<Instruction>();
        foreach (var block in Blocks)
            list.AddRange(block.Instructions);
        return list;
    }
}
