using Chrysalis.Cardano.Models.Cbor;
using ChrysalisAddress = Chrysalis.Cardano.Models.Plutus.Address;
using Chrysalis.Cbor;

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
[CborSerializable(CborType.Constr, Index = 0)]
public record ListingPayout(
    [CborProperty(0)]
    ChrysalisAddress Address, 
    
    [CborProperty(1)]
    CborUlong Amount
) : ICbor;