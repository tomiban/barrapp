using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_keeps_the_value_and_has_no_error()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_exposes_the_error_and_hides_the_value()
    {
        var error = Error.Validation("validation.failed", "Dato inválido.");

        var result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_without_an_error_is_invalid()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
    }
}
