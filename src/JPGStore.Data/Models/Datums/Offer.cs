using Chrysalis.Cbor;
using Chrysalis.Cardano.Models.Cbor;

namespace JPGStore.Data.Models.Datums;

/*
121_0([_
    h'1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5f',
    [_
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
            {h'': 121_0([_ 0, {h'': 1000000_2}])},
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
            {
                h'da3562fad43b7759f679970fb4e0ec07ab5bebe5c703043acda07a3c': 121_0([_
                    0,
                    {h'54656464794265617273436c756235383237': 1},
                ]),
            },
        ]),
    ],
])

d8799f581c1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5f9fd8799fd8799fd87a9f581c84cc25ea4c29951d40b443b95bbc5676bc425470f96376d1984af9abffd8799fd8799fd87a9f581c2c967f4bd28944b06462e13c5e3f5d5fa6e03f8567569438cd833e6dffffffffa140d8799f00a1401a000f4240ffffd8799fd8799fd8799f581cad6dda1cfe89c1091577e83b0ded3ae2d4cc641edf568d1e89cf6ea0ffd8799fd8799fd8799f581c58d91bc654dd9993b1e45727493c00a8cc11b2c55b81519db72d01feffffffffa140d8799f00a1401a000f4240ffffd8799fd8799fd8799f581c1765bf86c5c27dd863526a4e131d9b0aba69c34cf9d88f8f7c952a5fffd8799fd8799fd8799f581c2cee33060fb08404c8466d7568d66d96f0442cb91c7ab3a74ce7213cffffffffa1581cda3562fad43b7759f679970fb4e0ec07ab5bebe5c703043acda07a3cd8799f00a15254656464794265617273436c75623538323701ffffffff
*/
[CborSerializable(CborType.Constr, Index = 0)]
public record Offer(
    [CborProperty(0)]
    CborBytes OwnerPkh,

    [CborProperty(1)]
    CborIndefiniteList<OfferPayout> Payouts
) : ICbor;