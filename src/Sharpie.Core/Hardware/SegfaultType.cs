namespace Sharpie.Core.Hardware;

internal enum SegfaultType : byte
{
    OamCursorOutOfBounds = 0x01,
    ReservedRegionWrite = 0x02,
    ManualTrigger = 0xFF,
}

internal static class SegfaultExtensions
{
    public static string GetMessage(this SegfaultType type)
    {
        return type switch
        {
            SegfaultType.OamCursorOutOfBounds =>
                $"The OAM Cursor was set to an index above the maximum of {OamBank.MaxEntries - 1}",
            SegfaultType.ReservedRegionWrite =>
                $"There was an attempt to write to a protected region within memory ($FA20 - $FFFF)",
            _ => $"A manual segfault was triggered.",
        };
    }
}
