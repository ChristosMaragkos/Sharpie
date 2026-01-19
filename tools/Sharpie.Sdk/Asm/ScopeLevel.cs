namespace Sharpie.Sdk.Asm;

public class ScopeLevel
{
    public Dictionary<string, ushort> LabelAddresses { get; }
    public Dictionary<string, ushort> Constants { get; }
    public Dictionary<string, Dictionary<string, ushort>> Enums { get; }

    public ScopeLevel()
    {
        LabelAddresses = new();
        Constants = new();
        Enums = new();
    }

    public void DefineLabel(string name, ushort address) => LabelAddresses[name] = address;

    public bool TryDefineLabel(string name, ushort address) => LabelAddresses.TryAdd(name, address);

    public bool IsLabelDefined(string name) => LabelAddresses.ContainsKey(name);

    public void DefineConstant(string name, ushort value) => Constants[name] = value;

    public bool TryDefineConstant(string name, ushort value) => Constants.TryAdd(name, value);

    public bool IsConstantDefined(string name) => Constants.ContainsKey(name);

    public void DefineEnum(string name) => Enums[name] = new();

    public bool TryDefineEnum(string name) => Enums.TryAdd(name, new Dictionary<string, ushort>());

    public bool IsEnumDefined(string name) => Enums.ContainsKey(name);

    public void DefineEnumMember(string enumName, string memberName, ushort value) =>
        Enums[enumName][memberName] = value;

    public bool TryDefineEnumMember(string enumName, string memberName, ushort value) =>
        Enums.TryGetValue(enumName, out var members) && members.TryAdd(memberName, value);

    public bool IsEnumMemberDefined(string enumName, string memberName) =>
        Enums.TryGetValue(enumName, out var members) && members.ContainsKey(memberName);
}
