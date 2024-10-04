using Chrysalis.Cbor;
using ChrysalisAddress = Chrysalis.Cardano.Models.Plutus.Address;
using Chrysalis.Cardano.Models.Core;
using Chrysalis.Cardano.Models.Cbor;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_
    121_0([_
        122_0([_
            h'84cc25ea4c29951d40b443b95bbc5676bc425470f96376d1984af9ab',
        ]),
        121_0([_
            121_0([_
                122_0([_
                    h'2c967f4bd28944b06462e13c5e3f5d5fa6e03f8567569438cd833e6d',
                ]),
            ]),
        ]),
    ]),
    {h'': 121_0([_ 0, {h'': 1000000_2}])},
]),
*/
[CborSerializable(CborType.Constr, Index = 0)]
public record OfferPayout(
    [CborProperty(0)]
    ChrysalisAddress Address,
    
    [CborProperty(1)]
    CborMap<CborBytes, Token> PayoutValue
) : ICbor;