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
                h'08cd9c36460956cf8a75b02d87c368f7069323edef1d9880e53d915d',
            ]),
            122_0([]),
        ]),
        {
            h'd146eed96c50d9f1abd8e1a7ae81ab2dc3761675eda0ff99f36b0315': {h'': 5},
        },
    ]),
],
h'08cd9c36460956cf8a75b02d87c368f7069323edef1d9880e53d915d',
])

d8799f9fd8799fd8799fd8799f581c08cd9c36460956cf8a75b02d87c368f7069323edef1d9880e53d915dffd87a80ffa1581cd146eed96c50d9f1abd8e1a7ae81ab2dc3761675eda0ff99f36b0315a14005ffff581c08cd9c36460956cf8a75b02d87c368f7069323edef1d9880e53d915dff
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