using System.Formats.Cbor;
using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using CborSerialization;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_
    121_0([_
        121_0([_
            h'ad6dda1cfe89c1091577e83b0ded3ae2d4cc641edf568d1e89cf6ea0',
        ]),
        121_0([_
            121_0([_
                121_0([_
                    h'58d91bc654dd9993b1e45727493c00a8cc11b2c55b81519db72d01fe',
                ]),
            ]),
        ]),
    ]),
    1000000_2,
]),
*/
[CborSerialize(typeof(PayoutCborConvert))]
public record Payout(Address Address, ulong Amount) : IDatum;

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
        ulong amount = reader.ReadUInt64();
        reader.ReadEndArray();
        return new Payout(address, amount);
    }

    public void Write(ref CborWriter writer, Payout value)
    {
        writer.WriteTag((CborTag)121);
        writer.WriteStartArray(null);
        writer.WriteEncodedValue(CborConverter.Serialize(value.Address));
        writer.WriteUInt64(value.Amount);
        writer.WriteEndArray();
    }
}