using System.Formats.Cbor;
using Cardano.Sync.Data.Models.Datums;
using CborSerialization;

namespace JPGStore.Data.Models.Datums;

/*
{_
    h'': {_ h'': 10000},
    h'6e66745f706f6c6963795f31': {_ h'6e66745f31': 1},
}
*/
[CborSerialize(typeof(PayoutValueCborConvert))]
public record PayoutValue(Dictionary<string, Dictionary<string, ulong>> Value) : IDatum;

public class PayoutValueCborConvert : ICborConvertor<PayoutValue>
{
    public PayoutValue Read(ref CborReader reader)
    {
        Dictionary<string, Dictionary<string, ulong>> value = [];
        reader.ReadStartMap();
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            string policyId = Convert.ToHexString(reader.ReadByteString());
            reader.ReadStartMap();
            value.Add(policyId, []);
            while (reader.PeekState() != CborReaderState.EndMap)
            {
                string assetName = Convert.ToHexString(reader.ReadByteString());
                ulong assetValue = reader.ReadUInt64();
                value[policyId].Add(assetName, assetValue);
            }
            reader.ReadEndMap();
        }
        reader.ReadEndMap();

        return new PayoutValue(value);
    }



    public void Write(ref CborWriter writer, PayoutValue value)
    {
        writer.WriteStartMap(null);
        foreach (string? policyId in value.Value.Keys.ToList())
        {
            writer.WriteByteString(Convert.FromHexString(policyId));
            writer.WriteStartMap(null);
            foreach (string? assetName in value.Value[policyId].Keys.ToList())
            {
                writer.WriteByteString(Convert.FromHexString(assetName));
                writer.WriteUInt64(value.Value[policyId][assetName]);
            }
            writer.WriteEndMap();
        }
        writer.WriteEndMap();
    }
}