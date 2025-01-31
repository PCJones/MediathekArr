namespace Tvdb.Types;

public enum UpdateAction
{

    [System.Runtime.Serialization.EnumMember(Value = @"delete")]
    Delete = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"update")]
    Update = 1,

}