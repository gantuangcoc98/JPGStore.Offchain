using System.Formats.Cbor;
using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using CborSerialization;

namespace JPGStore.Data.Models.Datums;

/*
121([_
121([_ 121([_ h'00000000000001']), 122([])]),
{_
    h'': {_ h'': 10000},
    h'6e66745f706f6c6963795f31': {_ h'6e66745f31': 1},
},
])

d8799fd8799fd8799f4700000000000001ffd87a80ffa240a1401927104c6e66745f706f6c6963795f31a1456e66745f3101ff
*/
[CborSerialize(typeof(PayoutCborConvert))]
public record Payout(Address Address, PayoutValue Amount) : IDatum;

public class PayoutCborConvert : ICborConvertor<Payout>
{
    public Payout Read(ref CborReader reader)
    {
        CborTag tag = reader.ReadTag();
        if (tag != (CborTag)121)
        {
            throw new CborContentException("Unexpected tag");
        }

        reader.ReadStartArray();
        ReadOnlyMemory<byte> credential = reader.ReadEncodedValue();
        Address address = CborConverter.Deserialize<Address>(credential.Span.ToArray());
        PayoutValue amount = CborConverter.Deserialize<PayoutValue>(reader.ReadEncodedValue().Span.ToArray());
        reader.ReadEndArray();
        return new Payout(address, amount);
    }

    public void Write(ref CborWriter writer, Payout value)
    {
        writer.WriteTag((CborTag)121);
        writer.WriteStartArray(null);
        writer.WriteEncodedValue(CborConverter.Serialize(value.Address));
        writer.WriteEncodedValue(CborConverter.Serialize(value.Amount));
        writer.WriteEndArray();
    }
}