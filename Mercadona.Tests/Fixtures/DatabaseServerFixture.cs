using MysticMind.PostgresEmbed;
using System.Collections.Concurrent;

namespace Mercadona.Tests.Fixtures
{
    public class DatabaseServerFixture : IDisposable
    {
        const int RANDOM_PORT_MIN = 5500;
        const int RANDOM_PORT_MAX = 5601;

        private static readonly ConcurrentDictionary<int, DatabaseServerFixture> _usedPorts = new();

        private readonly PgServer _pgServer;

        public int PgPort { get; private set; }

        public DatabaseServerFixture()
        {
            PgPort = RandomPort;
            _pgServer = new PgServer("10.7.1", port: PgPort);
            _pgServer.Start();

            _usedPorts.TryAdd(PgPort, this);
        }

        public void Dispose()
        {
            _pgServer.Stop();
            _usedPorts.TryRemove(PgPort, out _);
        }

        private static int RandomPort
        {
            get
            {
                Random random = new();
                int randomPort = random.Next(RANDOM_PORT_MIN, RANDOM_PORT_MAX);
                while (_usedPorts.ContainsKey(randomPort))
                    randomPort = random.Next(RANDOM_PORT_MIN, RANDOM_PORT_MAX);
                return randomPort;
            }
        }
    }
}
