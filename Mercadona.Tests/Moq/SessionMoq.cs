using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Mercadona.Tests.Moq
{
    public class SessionMoq : ISession
    {
        private readonly string _id;
        private readonly bool _sessionShouldFail;
        private readonly ConcurrentDictionary<string, byte[]> _sessionData = new();

        public SessionMoq(bool sessionShouldFail = false)
        {
            _id = Guid.NewGuid().ToString();
            _sessionShouldFail = sessionShouldFail;
        }

        public bool IsAvailable => true;

        public string Id => _id;

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            if (_sessionShouldFail)
                throw new Exception("Test");
            _sessionData.Remove(key, out _);
        }

        public void Set(string key, byte[] value)
        {
            _sessionData.TryAdd(key, value);
        }

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
        {
            return _sessionData.TryGetValue(key, out value);
        }
    }
}
