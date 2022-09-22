﻿using Parsec.Attributes;
using Parsec.Shaiya.SData;

namespace Parsec.Shaiya.Monster;

public class DBMonsterTextRecord : IBinarySDataRecord
{
    [ShaiyaProperty]
    public long Id { get; set; }

    [ShaiyaProperty]
    [LengthPrefixedString(false)]
    public string Name { get; set; }
}
