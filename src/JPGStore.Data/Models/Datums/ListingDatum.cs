using System.Formats.Cbor;
using Cardano.Sync.Data.Models;
using Cardano.Sync.Data.Models.Datums;
using CborSerialization;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_
    [_
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
        121_0([_
            121_0([_
                121_0([_
                    h'1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5f',
                ]),
                121_0([_
                    121_0([_
                        121_0([_
                            h'2cee33060fb08404c8466d7568d66d96f0442cb91c7ab3a74ce7213c',
                        ]),
                    ]),
                ]),
            ]),
            13000000_2,
        ]),
    ],
    h'1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5f',
])

d8799f9fd8799fd8799fd8799f581cad6dda1cfe89c1091577e83b0ded3ae2d4cc641edf568d1e89cf6ea0ffd8799fd8799fd8799f581c58d91bc654dd9993b1e45727493c00a8cc11b2c55b81519db72d01feffffffff1a000f4240ffd8799fd8799fd8799f581c1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5fffd8799fd8799fd8799f581c2cee33060fb08404c8466d7568d66d96f0442cb91c7ab3a74ce7213cffffffff1a00c65d40ffff581c1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5fff
*/
[CborSerialize(typeof(DatumCborConvert))]
public record ListingDatum(List<Payout> Payouts, string OwnerPkh) : IDatum;
public class DatumCborConvert : ICborConvertor<ListingDatum>
{
    public ListingDatum Read(ref CborReader reader)
    {
        CborTag tag = reader.ReadTag();
        if (tag != (CborTag)121)
        {
            throw new CborContentException("Unexpected tag");
        }

        reader.ReadStartArray();
        List<Payout> payouts = [];
        reader.ReadStartArray();
        while (reader.PeekState() != CborReaderState.EndArray)
        {
            Payout payout = CborConverter.Deserialize<Payout>(reader.ReadEncodedValue().Span.ToArray());
            payouts.Add(payout);
        }
        reader.ReadEndArray();
        string ownerPkh = Convert.ToHexString(reader.ReadByteString());
        reader.ReadEndArray();
        return new ListingDatum(payouts, ownerPkh);
    }

    public void Write(ref CborWriter writer, ListingDatum value)
    {
        writer.WriteTag((CborTag)121);
        writer.WriteStartArray(null);
        writer.WriteStartArray(null);
        foreach (Payout payout in value.Payouts)
        {
            writer.WriteEncodedValue(CborConverter.Serialize(payout));
        }
        writer.WriteEndArray();
        writer.WriteByteString(Convert.FromHexString(value.OwnerPkh));
        writer.WriteEndArray();
    }
}