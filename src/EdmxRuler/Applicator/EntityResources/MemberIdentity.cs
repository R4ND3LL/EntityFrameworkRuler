using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Metadata; 

public class MemberIdentity : IEquatable<MemberIdentity> {
    public MemberIdentity(string name) {
    }

    public MemberIdentity(MemberInfo memberInfo) {
    }

    public bool IsNone() {
        return default;
    }

    public static MemberIdentity Create(string? name) {
        return default;
    }

    public static MemberIdentity Create(MemberInfo? memberInfo) {
        return default;
    }

    public string? Name
        => default;

    public MemberInfo? MemberInfo
        => default;

    public bool Equals(MemberIdentity other) {
        return false;
    }
}