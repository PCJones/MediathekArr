using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyHelpers.Extensions;

namespace MediathekArr.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Converts <see cref="DateTime"/> into <see cref="DateOnly"/>
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateOnly? ToDateOnly(this DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        return dateTime.Value.ToDateOnly();
    }

    /// <summary>
    /// Converts <see cref="DateTime"/> into <see cref="TimeOnly"/>
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static TimeOnly? ToTimeOnly(this DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        return dateTime.Value.ToTimeOnly();
    }
}
