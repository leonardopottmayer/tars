using Pottmayer.Tars.Caching.Options;
using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis.Options
{
    /// <summary>
    /// Options for the Redis caching provider, bound from configuration. Extends <see cref="CachingOptions"/>
    /// with the connection string and StackExchange.Redis tuning, and projects them into a
    /// <see cref="ConfigurationOptions"/> via <see cref="ToConfigurationOptions"/>.
    /// </summary>
    public sealed class RedisCachingOptions : CachingOptions
    {
        /// <summary>Default configuration section these options bind from (<c>Tars:Caching:Redis</c>).</summary>
        public const string SectionName = "Tars:Caching:Redis";

        /// <summary>Message reported when validation fails on application start.</summary>
        public const string ValidationErrorMessage =
            "Invalid RedisCachingOptions. ConnectionString is required; Database must be >= 0 when provided; timeouts/KeepAlive must be positive.";

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

        /// <summary>Number of connection retry attempts on initial connect.</summary>
        public int ConnectRetry { get; init; } = 3;

        /// <summary>Timeout for establishing the connection to Redis.</summary>
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>Timeout for synchronous Redis operations.</summary>
        public TimeSpan SyncTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>Interval between keep-alive pings that hold the connection open.</summary>
        public TimeSpan KeepAlive { get; init; } = TimeSpan.FromSeconds(60);

        /// <summary>Allows admin (potentially dangerous) operations on the connection, e.g. server commands.</summary>
        public bool AllowAdmin { get; init; } = false;

        /// <summary>
        /// Returns <c>true</c> when the options are internally consistent: the shared caching checks pass
        /// (see <see cref="CachingOptions.IsValid"/>), a connection string is present, the database index
        /// (when set) is non-negative, retry count is non-negative and all timeouts and keep-alive are
        /// strictly positive.
        /// </summary>
        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

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

        /// <summary>
        /// Builds the StackExchange.Redis <see cref="ConfigurationOptions"/> from these options, parsing the
        /// connection string and applying the configured retry, timeout, keep-alive, client name and database.
        /// </summary>
        /// <returns>The configured <see cref="ConfigurationOptions"/> used to connect the multiplexer.</returns>
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

