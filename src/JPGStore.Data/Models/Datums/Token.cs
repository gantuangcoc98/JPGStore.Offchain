using Chrysalis.Cardano.Models.Cbor;
using Chrysalis.Cardano.Models.Core;
using Chrysalis.Cbor;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_ 0, {h'': 1000000_2}])
*/
[CborSerializable(CborType.Constr, Index = 0)]
public record Token(
    [CborProperty(0)]
    CborInt TokenType,

    [CborProperty(1)]
    CborMap<CborBytes, CborUlong> Amount
) : ICbor;