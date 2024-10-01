using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JPGStore.Data.Utils;

public static class HashingUtils
{
    public static string GetHash(List<object> parameters)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parameters)));
        return Convert.ToHexString(bytes);
    }

    public static string GenerateHashFromComponents(params object[] components)
    {
        List<byte> bytes = [];
        
        foreach (object component in components)
        {
            byte[] componentBytes = ConvertComponentToBytes(component);
            bytes.AddRange(componentBytes);
        }

        byte[] hash = SHA256.HashData(bytes.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] ConvertComponentToBytes(object component)
    {
        return component switch
        {
            string strValue => Encoding.UTF8.GetBytes(strValue),
            int intValue => BitConverter.GetBytes(intValue),
            uint uintValue => BitConverter.GetBytes(uintValue),
            long longValue => BitConverter.GetBytes(longValue),
            ulong ulongValue => BitConverter.GetBytes(ulongValue),
            float floatValue => BitConverter.GetBytes(floatValue),
            double doubleValue => BitConverter.GetBytes(doubleValue),
            bool boolValue => BitConverter.GetBytes(boolValue),
            Enum enumValue => BitConverter.GetBytes(Convert.ToInt32(enumValue)),
            UIntPtr uPtr => BitConverter.GetBytes(uPtr.ToUInt64()),
            IntPtr ptr => BitConverter.GetBytes(ptr.ToInt64()),
            byte[] byteArrayValue => byteArrayValue,
            _ => throw new ArgumentException($"Unsupported type: {component.GetType()}"),
        };
    }
}