using Godot;

namespace SharpieStudio.Apps;

public interface IConfigurable<in T>
    where T : Resource
{
    void Configure(T data);
}
