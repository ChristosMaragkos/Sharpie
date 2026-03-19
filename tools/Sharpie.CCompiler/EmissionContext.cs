namespace Sharpie.CCompiler;

public sealed partial class SharpieEmitter
{
    private enum StorageType
    {
        Register,
        Stack,
    }

    // PLEASE MICROSOFT GIVE US DISCRIMINATED UNIONS PLEASE
    private readonly struct StorageLocation
    {
        public StorageType Type { get; }
        public int Value { get; }

        public StorageLocation(StorageType type, int value)
        {
            Type = type;
            Value = value;
        }
    }

    private sealed class EmissionContext
    {
        private readonly bool[] _tempInUse = new bool[TempRegisterEnd - TempRegisterStart + 1];

        public List<(
            int CallerOffset,
            StorageLocation Target,
            int Slots
        )> PendingStackArguments { get; } = new();

        public int TotalStackBytes { get; private set; }
        private int _currentStackOffset = 0;
        private int _nextLocalRegister = LocalRegisterStart;

        private static int _labelCount;

        public static string GenerateLabel(string prefix = "") => $"{prefix}_L{_labelCount++}";

        public Dictionary<string, StorageLocation> Locals { get; } = new(StringComparer.Ordinal);
        public HashSet<string> EscapedVariables { get; }

        // track callee saved registers (r8-r15)
        public List<int> UsedPreservedRegisters { get; } = new();

        public List<string> Instructions { get; } = new();

        public Stack<string> BreakLabels { get; } = new();
        public Stack<string> ContinueLabels { get; } = new();

        public List<string> ReadOnlyData { get; }
        public Dictionary<string, string> StringPool { get; }

        private readonly Dictionary<int, int> _tempSpillOfsets = new();

        public int GetSpillOffset(int reg)
        {
            if (!_tempSpillOfsets.TryGetValue(reg, out int offset))
            {
                offset = _currentStackOffset;
                _currentStackOffset += 2;
                TotalStackBytes = _currentStackOffset;
                _tempSpillOfsets[reg] = offset;
            }
            return offset;
        }

        public bool HasReturn { get; set; }

        public bool IsMain { get; set; }
        public string ReturnInstruction => IsMain ? "HALT" : "RET";

        public int HiddenRetPtrReg { get; set; } = -1; // which register is tracking the hidden return pointer (so struct returns can become void(struct Struct *ptr))

        public EmissionContext(
            HashSet<string> escapedVariables,
            List<string> readOnlyData,
            Dictionary<string, string> stringPool
        )
        {
            EscapedVariables = escapedVariables;
            ReadOnlyData = readOnlyData;
            StringPool = stringPool;
        }

        public void Emit(string asm)
        {
            Instructions.Add(asm);
        }

        public StorageLocation AllocateStorage(string name, bool forceStack, int bytes = 2)
        {
            if (!forceStack && _nextLocalRegister <= LocalRegisterEnd)
            {
                int reg = _nextLocalRegister++;
                UsedPreservedRegisters.Add(reg);

                var loc = new StorageLocation(StorageType.Register, reg);
                Locals[name] = loc;
                return loc;
            }
            else // fall back to the stack
            {
                int offset = _currentStackOffset;
                _currentStackOffset += bytes;
                TotalStackBytes = _currentStackOffset;

                var loc = new StorageLocation(StorageType.Stack, offset);
                Locals[name] = loc;
                return loc;
            }
        }

        public IEnumerable<string> GetPrologue()
        {
            foreach (var reg in UsedPreservedRegisters)
            {
                yield return $"PUSH r{reg}";
            }

            yield return "PUSH r15";

            if (TotalStackBytes > 0)
            {
                yield return "GETSP r15";
                yield return "MOV r6, r15";
                yield return $"LDI r7, {TotalStackBytes}";
                yield return "SUB r6, r7"; // SiX sEvEN
                yield return "SETSP r6";
                yield return "MOV r15, r6";
            }
            else
            {
                yield return "GETSP r15";
            }

            foreach (var pending in PendingStackArguments)
            {
                int argOffset =
                    TotalStackBytes + (UsedPreservedRegisters.Count * 2) + 4 + pending.CallerOffset;

                if (pending.Slots == 1)
                {
                    yield return $"MOV r6, r15";
                    yield return $"IADD r6, {argOffset}"; // Or use AccumulateOffset logic here
                    yield return "LDS r7, r6";

                    if (pending.Target.Type == StorageType.Register)
                        yield return $"MOV r{pending.Target.Value}, r7";
                    else
                    {
                        yield return $"MOV r6, r15";
                        yield return $"IADD r6, {pending.Target.Value}";
                        yield return $"STS r7, r6";
                    }
                }
                else
                {
                    yield return $"MOV r6, r15";
                    yield return $"IADD r6, {argOffset}";
                    yield return $"MOV r5, r15";
                    yield return $"IADD r5, {pending.Target.Value}";

                    for (int s = 0; s < pending.Slots; s++)
                    {
                        yield return "LDS r7, r6";
                        yield return "STS r7, r5";
                        if (s < pending.Slots - 1)
                        {
                            yield return "IADD r6, 2";
                            yield return "IADD r5, 2";
                        }
                    }
                }
            }
        }

        public IEnumerable<string> GetEpilogue()
        {
            yield return "SETSP r15";

            if (TotalStackBytes > 0)
            {
                yield return "MOV r6, r15";
                yield return $"LDI r7, {TotalStackBytes}";
                yield return "ADD r6, r7";
                yield return "SETSP r6";
            }

            yield return "POP r15";

            // Restore preserved registers (in REVERSE order)
            for (int i = UsedPreservedRegisters.Count - 1; i >= 0; i--)
            {
                yield return $"POP r{UsedPreservedRegisters[i]}";
            }
        }

        public TempLease AcquireTempRegister()
        {
            for (var i = 0; i < _tempInUse.Length; i++)
            {
                if (_tempInUse[i])
                    continue;

                _tempInUse[i] = true;
                return new TempLease(this, TempRegisterStart + i);
            }

            throw new InvalidOperationException(
                "Expression is too complex for temporary register budget."
            );
        }

        public List<int> GetActiveTempRegisters()
        {
            var active = new List<int>();
            for (int i = 0; i < _tempInUse.Length; i++)
            {
                if (_tempInUse[i])
                    active.Add(TempRegisterStart + i);
            }
            return active;
        }

        private void ReleaseTempRegister(int register)
        {
            var index = register - TempRegisterStart;
            if (index < 0 || index >= _tempInUse.Length)
                throw new InvalidOperationException(
                    $"Attempted to release invalid temp register r{register}."
                );

            _tempInUse[index] = false;
        }

        public readonly struct TempLease : IDisposable
        {
            private readonly EmissionContext _ctx;
            public int Value { get; }

            public TempLease(EmissionContext ctx, int value)
            {
                _ctx = ctx;
                Value = value;
            }

            public void Dispose()
            {
                _ctx.ReleaseTempRegister(Value);
            }
        }
    }
}
