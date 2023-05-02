using HttpContextMoq;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Mercadona.Tests.Extensions
{
    public static class HttpContextMockExt
    {
        public static HttpContextMock SetupSessionMoq(this HttpContextMock httpContextMock)
        {
            ISession session = httpContextMock.Session = new SessionMoq();
            httpContextMock.FeaturesMock.Mock
                .Setup((IFeatureCollection x) => x.Get<ISessionFeature>())
                .Returns(new SessionFeatureFake { Session = session });
            return httpContextMock;
        }
    }
}
