using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GarageGroup.Internal.Timesheet;

// The protected value layout: base64(nonce | tag | ciphertext).
// The user identifier is the associated data: it is not stored, but the value is decrypted only with the same identifier
internal sealed class AesGcmAgentTokenProtector : IAgentTokenProtector
{
    internal const int KeySize = 32;

    private const int NonceSize = 12;

    private const int TagSize = 16;

    private readonly byte[] key;

    internal AesGcmAgentTokenProtector(byte[] key)
    {
        if (key.Length is not KeySize)
        {
            throw new ArgumentException($"The token encryption key must contain {KeySize} bytes.", nameof(key));
        }

        this.key = key;
    }

    public string Protect(string value, long telegramUserId)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        var result = new byte[NonceSize + TagSize + plain.Length];

        var nonce = result.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, result.AsSpan(NonceSize + TagSize), result.AsSpan(NonceSize, TagSize), GetAssociatedData(telegramUserId));

        return Convert.ToBase64String(result);
    }

    public string? Unprotect(string protectedValue, long telegramUserId)
    {
        try
        {
            var source = Convert.FromBase64String(protectedValue);
            if (source.Length < NonceSize + TagSize)
            {
                return null;
            }

            var plain = new byte[source.Length - NonceSize - TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(
                nonce: source.AsSpan(0, NonceSize),
                ciphertext: source.AsSpan(NonceSize + TagSize),
                tag: source.AsSpan(NonceSize, TagSize),
                plaintext: plain,
                associatedData: GetAssociatedData(telegramUserId));

            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return null;
        }
    }

    private static byte[] GetAssociatedData(long telegramUserId)
        =>
        Encoding.UTF8.GetBytes("refresh-token:" + telegramUserId.ToString(CultureInfo.InvariantCulture));
}
