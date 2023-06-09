﻿using HttpContextMoq;
using Mercadona.Tests.Extensions;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using MimeDetective;
using Moq;
using System.Text;

namespace Mercadona.Tests;

public static class TestsHelper
{
    public static TokenValidationParameters TokenValidationParameters { get; private set; } =
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = "https://localhost:44387",
            ValidIssuer = "https://localhost:44387",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("JWTAuthenticationHIGHsecuredPasswordVVVp1OH7XzyrForTest")
            )
        };

    public static ProblemDetailsFactory ProblemDetailsFactory { get; private set; } =
        new ProblemDetailsFactoryMock();

    public static ContentInspector ContentInspector { get; private set; } =
        new ContentInspectorBuilder()
        {
            Definitions = MimeDetective.Definitions.Default.FileTypes.Images.All()
        }.Build();

    public static Mock<TService> GetServiceMock<TService>(Action<Mock<TService>>? setup = null)
        where TService : class
    {
        Mock<TService> mock = new();
        setup?.Invoke(mock);
        return mock;
    }

    public static TController CreateController<TController>(params object?[]? constructorArgs)
        where TController : ControllerBase
    {
        TController controller = (TController)
            Activator.CreateInstance(type: typeof(TController), args: constructorArgs)!;
        HttpContextMock httpContextMock = new() { RequestAborted = CancellationToken.None };
        httpContextMock.SetupSessionMoq();
        controller.ControllerContext.HttpContext = httpContextMock;
        controller.ProblemDetailsFactory = ProblemDetailsFactory;
        return controller;
    }
}
