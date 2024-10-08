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
using System.Text.RegularExpressions;
using CardanoSharp.Wallet.Utilities;

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
        
        Regex specialCharRegex = new(@"[:,](?!$)", RegexOptions.Compiled);

        foreach (JsonElement element in metadataValue.EnumerateArray())
        {
            if (element[0].GetInt64() == 30) continue;

            string? text = element[1].GetProperty("Text").GetString();

            if (text is null) continue;

            if (specialCharRegex.IsMatch(text)) continue;

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

        return datumCborHexList;
    }

    public static byte[]? GetStakeVerificationKeyFromAddress(ChrysalisAddress address)
    {
        if (address.StakeCredential is not Some<Inline<Credential>> someInlineCredential) return null;

        return ((VerificationKey)someInlineCredential.Value.Value).VerificationKeyHash.Value;
    }

    public static string? GetAddressBech32FromListingDatum(Listing listingDatum, IConfiguration configuration)
    {
        ChrysalisAddress? listingOwnerAddress = listingDatum.Payouts.Value
            .Select(po => po.Address)
            .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(listingDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        if (listingOwnerAddress is null) return null;

        byte[] listingOwnerAddressPaymentVKey = ((VerificationKey)listingOwnerAddress.PaymentCredential).VerificationKeyHash.Value;

        byte[]? listingOwnerAddressStakeVKey = GetStakeVerificationKeyFromAddress(listingOwnerAddress);

        if (listingOwnerAddressStakeVKey is null) return null;

        return AddressUtility.GetBaseAddress(
            listingOwnerAddressPaymentVKey,
            listingOwnerAddressStakeVKey,
            GetNetworkType(configuration)
        ).ToString();
    }

    public static string? GetAddressBech32FromOfferDatum(Offer offerDatum, IConfiguration configuration)
    {
        ChrysalisAddress? offerOwnerAddress = offerDatum.Payouts.Value
            .Select(po => po.Address)
            .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(offerDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        if (offerOwnerAddress is null) return null;

        byte[] offerOwnerAddressPaymentVKey = ((VerificationKey)offerOwnerAddress.PaymentCredential).VerificationKeyHash.Value;

        byte[]? offerOwnerAddressStakeVKey = GetStakeVerificationKeyFromAddress(offerOwnerAddress);

        if (offerOwnerAddressStakeVKey is null) return null;

        return AddressUtility.GetBaseAddress(
            offerOwnerAddressPaymentVKey,
            offerOwnerAddressStakeVKey,
            GetNetworkType(configuration)
        ).ToString();
    }
}