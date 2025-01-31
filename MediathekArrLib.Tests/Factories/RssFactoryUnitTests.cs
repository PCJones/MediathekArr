using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Models.Newznab;

namespace MediathekArr.Factories;

public class RssFactoryUnitTests
{
    [Fact]
    public async void Empty_Fact()
    {
        // Arrange
        Rss rssResult;

        // Act
        rssResult = RssFactory.Empty;

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