using CardanoSharp.Wallet.Models.Addresses;

namespace JPGStore.Data.Extensions;

public static class AddressExtension
{
    public static string? ToBech32(this byte[] address)
    {
        try
        {
            return new Address(address).ToString();
        }
        catch
        {
            return null;
        }
    }

    public static Address? ToAddress(this byte[] address)
    {
        try
        {
            return new Address(address);
        }
        catch
        {
            return null;
        }
    }
}