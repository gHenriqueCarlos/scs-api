using System.Security.Cryptography;
using System.Text;

namespace ScspApi.Services;

public static class OtpCrypto
{
    public static string GenerateNumericCode(int digits = 6)
    {
        // 000000–999999 (uniforme)
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        int value = BitConverter.ToInt32(bytes) & 0x7FFFFFFF;
        int mod = (int)Math.Pow(10, digits);
        return (value % mod).ToString(new string('0', digits));
    }

    public static string GenerateSalt(int size = 16)
    {
        var bytes = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(bytes);
    }

    public static string HashCode(string code, string salt)
    {
        var data = Encoding.UTF8.GetBytes(code + ":" + salt);
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash); // 64 hex
    }

    public static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        int result = 0;
        for (int i = 0; i < a.Length; i++) result |= a[i] ^ b[i];
        return result == 0;
    }
}
