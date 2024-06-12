namespace SS14.WebEverythingShared;

[AttributeUsage(AttributeTargets.Field)]
public sealed class AuditEntryTypeAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

public static class AuditEntryHelper
{
    public static (Dictionary<TEnum, Type>, Dictionary<Type, TEnum>) CreateEntryMapping<TEnum, TEntry>()
        where TEnum : struct, Enum
        where TEntry : class
    {
        var enumToType = new Dictionary<TEnum, Type>();
        var typeToEnum = new Dictionary<Type, TEnum>();

        var type = typeof(TEnum);

        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();

            var field = type.GetMember(name)[0];
            var attribute = (AuditEntryTypeAttribute?) Attribute.GetCustomAttribute(field, typeof(AuditEntryTypeAttribute));

            if (attribute == null)
                throw new InvalidOperationException($"{name} is missing entry type");

            if (!attribute.Type.IsAssignableTo(typeof(TEntry)))
                throw new InvalidOperationException($"{attribute.Type} must inherit entry type");

            enumToType.Add(value, attribute.Type);
            typeToEnum.Add(attribute.Type, value);
        }

        return (enumToType, typeToEnum);
    }
}
