using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Models.Newznab;
using MediathekArr.Utilities;

namespace MediathekArr.Utilities;

public class NewznabUtilsUnitTests
{
    [Fact]
    public async Task GetEmptyRssResult_Fact()
    {
        // Arrange
        Rss rssResult;

        // Act
        rssResult = NewznabUtils.GetEmptyRssResult();

        // Assert
        rssResult.Should().NotBeNull();

        // Assert Channel
        rssResult.Channel.Should().NotBeNull();
        rssResult.Channel.Title.Should().BeEquivalentTo("MediathekArr");
        rssResult.Channel.Description.Should().BeEquivalentTo("MediathekArr API results");
        rssResult.Channel.Items.Should().BeEmpty();

        // Assert Channel Response
        rssResult.Channel.Response.Should().NotBeNull();
        rssResult.Channel.Response.Offset.Should().Be(0);
        rssResult.Channel.Response.Total.Should().Be(0);
    }
}