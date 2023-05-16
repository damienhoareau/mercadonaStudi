using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Mercadona.Tests.Moq
{
    public class SessionMoq : ISession
    {
        private readonly string _id;
        private readonly ConcurrentDictionary<string, byte[]> _sessionData = new();

        public SessionMoq()
        {
            _id = Guid.NewGuid().ToString();
        }

        public bool IsAvailable => true;

        public string Id => _id;

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public void Clear()
        {
            _sessionData.Clear();
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
            _sessionData.Remove(key, out _);
        }

        public void Set(string key, byte[] value)
        {
            if (_sessionData.ContainsKey(key))
                _sessionData.TryRemove(key, out _);
            _sessionData.TryAdd(key, value);
        }

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
        {
            return _sessionData.TryGetValue(key, out value);
        }
    }
}
