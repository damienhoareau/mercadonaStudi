using Mercadona.Backend.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mercadona.Tests.Moq;

public class AuthAutoValidateAntiforgeryTokenFilterMock : AuthAutoValidateAntiforgeryTokenFilter
{
    public AuthAutoValidateAntiforgeryTokenFilterMock(IAntiforgery antiforgery) : base(antiforgery)
    { }

    private bool _shouldValidate;

    public void SetShouldValidate(bool value) => _shouldValidate = value;

    protected override bool ShouldValidate(AuthorizationFilterContext context) => _shouldValidate;

    public bool CallShouldValidate(AuthorizationFilterContext context) =>
        base.ShouldValidate(context);
}
