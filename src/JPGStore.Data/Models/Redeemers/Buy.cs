using Chrysalis.Cbor;
using Chrysalis.Cardano.Models.Cbor;

namespace Crashr.Data.Models.Redeemers;

/*
121_0([_ 0])

d8799f00ff
*/
[CborSerializable(CborType.Constr, Index = 0)]
public record Buy(
    [CborProperty(0)]
    CborInt Offset
) : ICbor;