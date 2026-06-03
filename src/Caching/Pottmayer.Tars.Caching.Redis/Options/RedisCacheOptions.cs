using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis.Options
{
    public sealed class RedisCacheOptions
    {
        public const string SectionName = "Tars:Caching:Redis";

        public const string ValidationErrorMessage =
            "Invalid RedisCacheOptions. ConnectionString is required; Database must be >= 0 when provided; timeouts/KeepAlive must be positive.";

        /// <summary>
        /// Redis connection string (e.g. "localhost:6379,password=...,ssl=True,abortConnect=False").
        /// </summary>
        public string ConnectionString { get; init; } = string.Empty;

        /// <summary>
        /// Logical database index. Null means "use StackExchange.Redis default".
        /// </summary>
        public int? Database { get; init; } = null;

        /// <summary>
        /// Optional client name (useful for diagnostics on Redis server).
        /// </summary>
        public string? ClientName { get; init; } = null;

        /// <summary>
        /// Prefer resilient startup: do not abort on initial connect failures.
        /// </summary>
        public bool AbortOnConnectFail { get; init; } = false;

        public int ConnectRetry { get; init; } = 3;

        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

        public TimeSpan SyncTimeout { get; init; } = TimeSpan.FromSeconds(5);

        public TimeSpan KeepAlive { get; init; } = TimeSpan.FromSeconds(60);

        public bool AllowAdmin { get; init; } = false;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                return false;

            if (Database is not null && Database < 0)
                return false;

            if (ConnectRetry < 0)
                return false;

            if (ConnectTimeout <= TimeSpan.Zero)
                return false;

            if (SyncTimeout <= TimeSpan.Zero)
                return false;

            if (KeepAlive <= TimeSpan.Zero)
                return false;

            return true;
        }

        public ConfigurationOptions ToConfigurationOptions()
        {
            var cfg = ConfigurationOptions.Parse(ConnectionString, ignoreUnknown: true);

            cfg.AbortOnConnectFail = AbortOnConnectFail;
            cfg.ConnectRetry = ConnectRetry;
            cfg.ConnectTimeout = (int)ConnectTimeout.TotalMilliseconds;
            cfg.SyncTimeout = (int)SyncTimeout.TotalMilliseconds;
            cfg.KeepAlive = (int)KeepAlive.TotalSeconds;
            cfg.AllowAdmin = AllowAdmin;

            if (!string.IsNullOrWhiteSpace(ClientName))
                cfg.ClientName = ClientName;

            if (Database is not null)
                cfg.DefaultDatabase = Database;

            return cfg;
        }
    }
}

