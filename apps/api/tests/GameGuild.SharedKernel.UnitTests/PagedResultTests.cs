using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class PagedResultTests
{
    [Fact]
    public void Constructor_ShouldSetOffsetProperties()
    {
        var items = new[] { 1, 2, 3 };
        var result = new PagedResult<int>(items, 50, 10, 3);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(50);
        result.Skip.Should().Be(10);
        result.Take.Should().Be(3);
    }

    [Fact]
    public void Constructor_ShouldComputePageNumber()
    {
        // skip=10, take=5 → page 3
        var result = new PagedResult<int>([], 50, 10, 5);
        result.PageNumber.Should().Be(3);
    }

    [Fact]
    public void Constructor_ShouldHandleZeroTake()
    {
        var result = new PagedResult<int>([], 50, 0, 0);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public void FromPage_ShouldComputeSkip()
    {
        var result = PagedResult<int>.FromPage(new[] { 1 }, 50, 3, 10);

        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.Skip.Should().Be(20);
    }

    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        var result = PagedResult<int>.Empty(5);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public void TotalPages_ShouldCeilDivide()
    {
        var result = new PagedResult<int>([], 11, 0, 5);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_ShouldReturnZero_WhenPageSizeIsZero()
    {
        var result = new PagedResult<int>([], 10, 0, 0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasNextPage_ShouldBeTrue_WhenNotOnLastPage()
    {
        var result = new PagedResult<int>([], 20, 0, 10);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeFalse_WhenOnLastPage()
    {
        var result = new PagedResult<int>([], 20, 10, 10);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalse_WhenOnFirstPage()
    {
        var result = new PagedResult<int>([], 20, 0, 10);
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrue_WhenNotOnFirstPage()
    {
        var result = new PagedResult<int>([], 20, 10, 10);
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void TotalPages_ExactDivision()
    {
        var result = new PagedResult<int>([], 20, 0, 10);
        result.TotalPages.Should().Be(2);
    }
}
