using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace Mercadona.Tests.Extensions;

public static class FluentAssertionsExt
{
    public static void BeActionResult<TResult>(
        this ObjectAssertions result,
        Action<TResult>? typedAssertion = null
    ) where TResult : IActionResult
    {
        result.BeOfType<TResult>();
        if (typedAssertion != null)
            typedAssertion((TResult)result.Subject);
    }

    public static void BeActionResult<TResult, TValueResult>(
        this ObjectAssertions result,
        TValueResult expectedResult
    ) where TResult : ObjectResult
    {
        result.BeOfType<TResult>();
        ((TResult)result.Subject).Value.Should().BeAssignableTo<TValueResult>();
        TValueResult resultValue = (TValueResult)((TResult)result.Subject).Value!;
        resultValue.Should().BeEquivalentTo(expectedResult);
    }

    public static void BeProblemResult(this ObjectAssertions result, int? status, string? detail)
    {
        result.BeOfType<ObjectResult>();
        ((ObjectResult)result.Subject).Value.Should().BeOfType<ProblemDetails>();
        ProblemDetails problemDetails = (ProblemDetails)((ObjectResult)result.Subject).Value!;
        problemDetails.Status.Should().Be(status);
        problemDetails.Detail.Should().Be(detail);
    }
}
