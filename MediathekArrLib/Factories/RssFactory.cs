using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Models.Newznab;

namespace MediathekArr.Factories;

public static class RssFactory
{
    /// <summary>
    /// Empty RSS response with MediathekArr flavour
    /// </summary>
    public static Rss Empty => new()
    {
        // TODO: Further refactoring to decouple this into dedicated factories
        Channel = new Channel
        {
            Title = "MediathekArr",
            Description = "MediathekArr API results",
            Response = new Response
            {
                Offset = 0,
                Total = 0
            },
            Items = []
        }
    };
}