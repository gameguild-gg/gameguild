using FluentAssertions;

namespace GameGuild.SharedKernel.UnitTests.Models;

public class CursorPaginationTests
{
    [Fact]
    public void EncodeCursor_WithIdOnly_ShouldProduceBase64()
    {
        var id = Guid.NewGuid();
        var cursor = CursorPagination.EncodeCursor(id);

        cursor.Should().NotBeNullOrEmpty();
        // Should be valid base64
        var act = () => Convert.FromBase64String(cursor);
        act.Should().NotThrow();
    }

    [Fact]
    public void EncodeCursor_WithTimestamp_ShouldIncludeTimestamp()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var cursor = CursorPagination.EncodeCursor(id, timestamp);

        cursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DecodeCursor_RoundTrip_IdOnly()
    {
        var id = Guid.NewGuid();
        var cursor = CursorPagination.EncodeCursor(id);

        var (decodedId, decodedTs) = CursorPagination.DecodeCursor(cursor);

        decodedId.Should().Be(id);
        decodedTs.Should().BeNull();
    }

    [Fact]
    public void DecodeCursor_RoundTrip_WithTimestamp()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var cursor = CursorPagination.EncodeCursor(id, timestamp);

        var (decodedId, decodedTs) = CursorPagination.DecodeCursor(cursor);

        decodedId.Should().Be(id);
        decodedTs.Should().NotBeNull();
    }

    [Fact]
    public void DecodeCursor_InvalidBase64_ShouldReturnEmptyGuid()
    {
        var (id, ts) = CursorPagination.DecodeCursor("not-valid-base64!!!");

        id.Should().Be(Guid.Empty);
        ts.Should().BeNull();
    }

    [Fact]
    public void DecodeCursor_ValidBase64ButBadGuid_ShouldReturnEmptyGuid()
    {
        var cursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("not-a-guid"));

        var (id, ts) = CursorPagination.DecodeCursor(cursor);

        id.Should().Be(Guid.Empty);
        ts.Should().BeNull();
    }

    [Fact]
    public void CreateResult_WhenItemsLessThanRequested_ShouldNotHaveMore()
    {
        var items = new List<TestItem>
        {
            new(Guid.NewGuid(), "A"),
            new(Guid.NewGuid(), "B")
        };

        var result = CursorPagination.CreateResult(
            items, requestedCount: 5,
            idSelector: x => x.Id);

        result.Items.Should().HaveCount(2);
        result.HasMore.Should().BeFalse();
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public void CreateResult_WhenItemsExceedRequested_ShouldHaveMore()
    {
        var items = new List<TestItem>
        {
            new(Guid.NewGuid(), "A"),
            new(Guid.NewGuid(), "B"),
            new(Guid.NewGuid(), "C"),
            new(Guid.NewGuid(), "D")
        };

        var result = CursorPagination.CreateResult(
            items, requestedCount: 3,
            idSelector: x => x.Id);

        result.Items.Should().HaveCount(3);
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateResult_WithTimestampSelector_ShouldEncodeTimestamp()
    {
        var ts = DateTime.UtcNow;
        var items = new List<TestItem>
        {
            new(Guid.NewGuid(), "A"),
            new(Guid.NewGuid(), "B"),
            new(Guid.NewGuid(), "C"),
            new(Guid.NewGuid(), "D")
        };

        var result = CursorPagination.CreateResult(
            items, requestedCount: 3,
            idSelector: x => x.Id,
            timestampSelector: _ => ts);

        result.NextCursor.Should().NotBeNullOrEmpty();
        var (_, decodedTs) = CursorPagination.DecodeCursor(result.NextCursor!);
        decodedTs.Should().NotBeNull();
    }

    [Fact]
    public void CreateResult_WithTotalCount_ShouldSetIt()
    {
        var items = new List<TestItem> { new(Guid.NewGuid(), "A") };

        var result = CursorPagination.CreateResult(
            items, requestedCount: 10,
            idSelector: x => x.Id,
            totalCount: 42);

        result.TotalCount.Should().Be(42);
    }

    [Fact]
    public void CreateResult_EmptyItems_ShouldReturnEmpty()
    {
        var items = new List<TestItem>();

        var result = CursorPagination.CreateResult(
            items, requestedCount: 10,
            idSelector: x => x.Id);

        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
        result.NextCursor.Should().BeNull();
    }

    private record TestItem(Guid Id, string Name);
}

public class SortingParamsTests
{
    [Fact]
    public void Defaults_ShouldBeDescending()
    {
        var sort = new SortingParams();

        sort.Order.Should().Be("desc");
        sort.IsDescending.Should().BeTrue();
        sort.Sort.Should().BeNull();
    }

    [Fact]
    public void IsDescending_WhenAsc_ShouldBeFalse()
    {
        var sort = new SortingParams { Order = "asc" };

        sort.IsDescending.Should().BeFalse();
    }

    [Fact]
    public void IsDescending_CaseInsensitive()
    {
        var sort = new SortingParams { Order = "DESC" };

        sort.IsDescending.Should().BeTrue();
    }
}

public class PaginationParamsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var pagination = new PaginationParams();

        pagination.Skip.Should().Be(0);
        pagination.Take.Should().Be(20);
        pagination.Cursor.Should().BeNull();
    }

    [Fact]
    public void Settable_Properties()
    {
        var pagination = new PaginationParams { Skip = 10, Take = 50, Cursor = "abc" };

        pagination.Skip.Should().Be(10);
        pagination.Take.Should().Be(50);
        pagination.Cursor.Should().Be("abc");
    }
}

public class ListQueryParamsTests
{
    [Fact]
    public void Defaults_ShouldInheritPagination()
    {
        var query = new ListQueryParams();

        query.Skip.Should().Be(0);
        query.Take.Should().Be(20);
        query.Order.Should().Be("desc");
        query.IsDescending.Should().BeTrue();
        query.Sort.Should().BeNull();
        query.Search.Should().BeNull();
    }

    [Fact]
    public void IsDescending_WhenAscOrder_ShouldBeFalse()
    {
        var query = new ListQueryParams { Order = "asc" };

        query.IsDescending.Should().BeFalse();
    }

    [Fact]
    public void Search_ShouldBeSettable()
    {
        var query = new ListQueryParams { Search = "hello", Sort = "Name" };

        query.Search.Should().Be("hello");
        query.Sort.Should().Be("Name");
    }
}

public class CursorPagedResultTests
{
    [Fact]
    public void Defaults_ShouldBeEmpty()
    {
        var result = new CursorPagedResult<string>();

        result.Items.Should().BeEmpty();
        result.NextCursor.Should().BeNull();
        result.PreviousCursor.Should().BeNull();
        result.HasMore.Should().BeFalse();
        result.TotalCount.Should().BeNull();
    }

    [Fact]
    public void Settable_Properties()
    {
        var result = new CursorPagedResult<int>
        {
            Items = new List<int> { 1, 2, 3 },
            NextCursor = "next",
            PreviousCursor = "prev",
            HasMore = true,
            TotalCount = 100
        };

        result.Items.Should().HaveCount(3);
        result.NextCursor.Should().Be("next");
        result.PreviousCursor.Should().Be("prev");
        result.HasMore.Should().BeTrue();
        result.TotalCount.Should().Be(100);
    }
}
