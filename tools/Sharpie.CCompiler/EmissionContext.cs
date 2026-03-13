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

        public int TotalStackBytes { get; }

        private int _currentStackOffset = 0;

        private int _nextLocalRegister = LocalRegisterStart;

        private static int _labelCount;

        public static string GenerateLabel(string prefix = "") => $"{prefix}_L{_labelCount++}";

        public Dictionary<string, int> Locals { get; } = new(StringComparer.Ordinal);
        public List<string> Instructions { get; } = [];
        public bool HasReturn { get; set; }

        public bool IsMain { get; set; }
        public string ReturnInstruction => IsMain ? "HALT" : "RET";

        public EmissionContext(int totalStackBytes)
        {
            TotalStackBytes = totalStackBytes;
        }

        public void Emit(string asm)
        {
            Instructions.Add(asm);
        }

        public int AllocateStackSpace(int bytes = 2)
        {
            int offset = _currentStackOffset;
            _currentStackOffset += bytes;
            return offset;
        }

        public void EmitEpilogue()
        {
            if (TotalStackBytes > 0)
            {
                Emit($"LDI r1, {TotalStackBytes}");
                Emit($"CALL SYS_FREE_STACKFRAME");
            }
        }

        public int AllocateLocalRegister()
        {
            if (_nextLocalRegister > LocalRegisterEnd)
                throw new InvalidOperationException(
                    "Too many local variables for register-only MVP backend."
                );

            return _nextLocalRegister++;
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
