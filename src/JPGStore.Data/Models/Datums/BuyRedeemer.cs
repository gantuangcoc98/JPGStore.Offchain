using System.Formats.Cbor;
using Cardano.Sync.Data.Models.Datums;
using CborSerialization;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_ 0])

d8799f00ff
*/
[CborSerialize(typeof(BuyRedeemerCborConvert))]
public record BuyRedeemer(ulong Offset) : IDatum;

public class BuyRedeemerCborConvert : ICborConvertor<BuyRedeemer>
{
    public BuyRedeemer Read(ref CborReader reader)
    {
        CborTag tag = reader.ReadTag();
        if (tag != (CborTag)121)
        {
            throw new CborContentException("Unexpected tag");
        }

        reader.ReadStartArray();
        ulong offset = reader.ReadUInt64();
        reader.ReadEndArray();
        return new BuyRedeemer(offset);
    }

    public void Write(ref CborWriter writer, BuyRedeemer offset)
    {
        writer.WriteTag((CborTag)121);
        writer.WriteStartArray(null);
        writer.WriteUInt64(offset.Offset);
        writer.WriteEndArray();
    }
}