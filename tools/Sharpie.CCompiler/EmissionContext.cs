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

        public int TotalStackBytes { get; private set; }
        private int _currentStackOffset = 0;
        private int _nextLocalRegister = LocalRegisterStart;

        private static int _labelCount;

        public static string GenerateLabel(string prefix = "") => $"{prefix}_L{_labelCount++}";

        public Dictionary<string, StorageLocation> Locals { get; } = new(StringComparer.Ordinal);
        public HashSet<string> EscapedVariables { get; }

        // track callee saved registers (r8-r15)
        public List<int> UsedPreservedRegisters { get; } = new();

        public List<string> Instructions { get; } = [];
        public bool HasReturn { get; set; }

        public bool IsMain { get; set; }
        public string ReturnInstruction => IsMain ? "HALT" : "RET";

        public EmissionContext(HashSet<string> escapedVariables)
        {
            EscapedVariables = escapedVariables;
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

            if (TotalStackBytes > 0)
            {
                yield return $"LDI r1, {TotalStackBytes}";
                yield return "CALL SYS_ALLOC_STACKFRAME";
            }
        }

        public IEnumerable<string> GetEpilogue()
        {
            if (TotalStackBytes > 0)
            {
                yield return $"LDI r1, {TotalStackBytes}";
                yield return "CALL SYS_FREE_STACKFRAME";
            }

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
