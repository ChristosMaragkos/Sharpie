namespace Sharpie.Sdk.Asm;

public class ScopeLevel
{
    public ScopeLevel? Parent { get; init; }
    public int Id { get; init; }

    public Dictionary<string, ushort> LabelAddresses { get; }
    public Dictionary<string, ushort> Constants { get; }
    public Dictionary<string, Dictionary<string, ushort>> Enums { get; }

    public ScopeLevel(ScopeLevel? parent, int id)
    {
        Id = id;
        Parent = parent;
        LabelAddresses = [];
        Constants = [];
        Enums = [];
    }

    public void DefineLabel(string name, ushort address) => LabelAddresses[name] = address;

    public bool TryDefineLabel(string name, ushort address) => LabelAddresses.TryAdd(name, address);

    public bool IsLabelDefined(string name) => LabelAddresses.ContainsKey(name);

    public void DefineConstant(string name, ushort value) => Constants[name] = value;

    public bool TryDefineConstant(string name, ushort value) => Constants.TryAdd(name, value);

    public bool IsConstantDefined(string name) => Constants.ContainsKey(name);

    public void DefineEnum(string name) => Enums[name] = [];

    public bool TryDefineEnum(string name) => Enums.TryAdd(name, []);

    public bool IsEnumDefined(string name) => Enums.ContainsKey(name);

    public void DefineEnumMember(string enumName, string memberName, ushort value) =>
        Enums[enumName][memberName] = value;

    public bool TryDefineEnumMember(string enumName, string memberName, ushort value) =>
        Enums.TryGetValue(enumName, out var members) && members.TryAdd(memberName, value);

    public bool IsEnumMemberDefined(string enumName, string memberName) =>
        Enums.TryGetValue(enumName, out var members) && members.ContainsKey(memberName);

    public bool TryResolveLabel(string name, out ushort value)
    {
        if (LabelAddresses.TryGetValue(name, out value))
            return true;
        return Parent?.TryResolveLabel(name, out value) ?? false;
    }

    public bool TryResolveConstant(string name, out ushort value)
    {
        if (Constants.TryGetValue(name, out value))
            return true;
        return Parent?.TryResolveConstant(name, out value) ?? false;
    }

    public bool TryResolveEnum(string name)
    {
        if (Enums.TryGetValue(name, out _))
            return true;
        return Parent?.TryResolveEnum(name) ?? false;
    }

    public bool TryResolveEnumMember(string enumName, string memberName, out ushort value)
    {
        if (
            Enums.TryGetValue(enumName, out var members)
            && members.TryGetValue(memberName, out value)
        )
            return true;
        value = 0;
        return Parent?.TryResolveEnumMember(enumName, memberName, out value) ?? false;
    }

    public override string ToString()
    {
        return string.Join(' ', Constants.Keys);
    }
}
