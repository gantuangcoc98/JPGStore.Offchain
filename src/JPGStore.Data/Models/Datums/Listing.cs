using Chrysalis.Cardano.Models.Cbor;
using Chrysalis.Cbor;

namespace JPGStore.Data.Models.Datums;

/*
ListingDatum
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
[CborSerializable(CborType.Constr, Index = 0)]
public record Listing(
    [CborProperty(0)]
    CborIndefiniteList<ListingPayout> Payouts, 

    [CborProperty(1)]
    CborBytes OwnerPkh
) : ICbor;