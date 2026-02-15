using FluentAssertions;
using GameGuild.API.Controllers;

namespace GameGuild.API.UnitTests.Core;

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Success_ShouldSetProperties()
    {
        var response = new ApiResponse<string>
        {
            Success = true,
            Data = "hello",
            Message = "ok",
            Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        response.Success.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Message.Should().Be("ok");
        response.Errors.Should().BeNull();
        response.Timestamp.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ApiResponse_Error_ShouldSetProperties()
    {
        var errors = new { field = "Name", message = "Required" };
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        };

        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Errors.Should().NotBeNull();
    }

    [Fact]
    public void PagedApiResponse_HasNextPage_WhenPageLessThanTotal_ShouldBeTrue()
    {
        var response = new PagedApiResponse<int>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        response.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedApiResponse_HasNextPage_WhenPageEqualsTotal_ShouldBeFalse()
    {
        var response = new PagedApiResponse<int>
        {
            Page = 3,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedApiResponse_HasNextPage_WhenPageExceedsTotal_ShouldBeFalse()
    {
        var response = new PagedApiResponse<int>
        {
            Page = 5,
            TotalPages = 3
        };

        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedApiResponse_HasPreviousPage_WhenPageGreaterThanOne_ShouldBeTrue()
    {
        var response = new PagedApiResponse<int> { Page = 2 };

        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedApiResponse_HasPreviousPage_WhenPageIsOne_ShouldBeFalse()
    {
        var response = new PagedApiResponse<int> { Page = 1 };

        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedApiResponse_InheritsApiResponse()
    {
        var response = new PagedApiResponse<string>
        {
            Success = true,
            Data = new[] { "a", "b" },
            Page = 1,
            PageSize = 5,
            TotalCount = 2,
            TotalPages = 1,
            Message = "fetched"
        };

        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Message.Should().Be("fetched");
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedApiResponse_EmptyResult_ShouldWork()
    {
        var response = new PagedApiResponse<int>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 0,
            TotalPages = 0,
            Data = Enumerable.Empty<int>()
        };

        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeFalse();
        response.TotalCount.Should().Be(0);
    }
}
