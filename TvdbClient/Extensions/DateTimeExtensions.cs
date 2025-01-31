using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Extensions;

/// <summary>
/// Extensions for <see cref="DateTime"/>
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Check whether a Date is in the Past
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static bool IsInThePast(this DateTime date) => date < DateTime.Now;
}