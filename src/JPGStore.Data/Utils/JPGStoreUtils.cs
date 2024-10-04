using System.Text;
using System.Text.Json;
using CardanoSharp.Wallet.Enums;
using Microsoft.Extensions.Configuration;
using JPGStore.Data.Models.Enums;
using JPGStore.Data.Models.Datums;
using Chrysalis.Cardano.Models.Plutus;
using ChrysalisAddress = Chrysalis.Cardano.Models.Plutus.Address;
using Chrysalis.Cbor;
using CborSerialization;

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

    public static List<string> MapMetadataToCborHexList(JsonElement metadataValue)
    {
        List<string> datumCborHexList = [];
        StringBuilder datumCborHex = new();
        
        foreach (JsonElement element in metadataValue.EnumerateArray())
        {
            if (element[0].GetInt64() == 30) continue;

            string? text = element[1].GetProperty("Text").GetString();

            if (text != null)
            {
                if (text.EndsWith(','))
                {
                    datumCborHex.Append(text.TrimEnd(','));
                    datumCborHexList.Add(datumCborHex.ToString());
                    datumCborHex.Clear();
                }
                else
                {
                    datumCborHex.Append(text);
                }
            }
        }

        return datumCborHexList;
    }

    public static ChrysalisAddress? GetOwnerCredentialFromDatum(byte[] datum, TransactionDatum datumType)
    {
        switch (datumType)
        {
            case TransactionDatum.Listing:
                Listing listingDatum = CborSerializer.Deserialize<Listing>(datum) ?? throw new CborException($"Unable to derserialize listing datum: {Convert.ToHexString(datum)}");
                return listingDatum.Payouts.Value
                    .Select(po => po.Address)
                    .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(listingDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();
            case TransactionDatum.Offer:
                Offer offerDatum = CborSerializer.Deserialize<Offer>(datum) ?? throw new CborException($"Unable to derserialize offer datum: {Convert.ToHexString(datum)}");
                return offerDatum.Payouts.Value
                    .Select(po => po.Address)
                    .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(offerDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();
            default:
                return null;
        }
    }
}