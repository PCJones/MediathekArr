using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Types;

public enum Sort
{

    [System.Runtime.Serialization.EnumMember(Value = @"score")]
    Score = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"firstAired")]
    FirstAired = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"name")]
    Name = 2,

}

public enum Sort2
{

    [System.Runtime.Serialization.EnumMember(Value = @"score")]
    Score = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"firstAired")]
    FirstAired = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"lastAired")]
    LastAired = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"name")]
    Name = 3,

}
