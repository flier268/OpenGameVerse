using FluentAssertions;
using OpenGameVerse.Core.Common;

namespace OpenGameVerse.Core.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Result_Success_ShouldIndicateSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_ShouldIncludeErrorMessage()
    {
        // Act
        var result = Result.Failure("Something went wrong");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Something went wrong");
    }

    [Fact]
    public void ResultT_Success_ShouldReturnValue()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultT_Failure_ShouldIncludeError()
    {
        // Act
        var result = Result<string>.Failure("Not found");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("Not found");
    }
}
