using System.ComponentModel;
using System.Reflection;

public class StringToEnum

{
    public static T? ParseEnumFromDescription<T>(string value) where T : struct, Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null && attribute.Description.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return (T)field.GetValue(null);
            }
        }
        return null; // Return null if no match
    }

    public static string GetEnumDescription<T>(T value) where T : Enum
    {
        var field = typeof(T).GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}