using CardanoSharp.Wallet.Enums;
using Microsoft.Extensions.Configuration;

namespace JPGStore.Data.Utils;

public static class JPGStoreUtils
{
    public static NetworkType GetNetworkType(IConfiguration configuration)
    {
        return configuration.GetValue<int>("CardanoNetworkMagic") switch
        {
            764824073 => NetworkType.Mainnet,
            1 => NetworkType.Preprod,
            2 => NetworkType.Preview,
            _ => throw new NotImplementedException()
        };
    }
}