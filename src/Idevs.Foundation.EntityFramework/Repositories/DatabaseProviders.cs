namespace Idevs.Foundation.EntityFramework.Repositories;

/// <summary>
/// Constants for database provider names used across repository implementations.
/// </summary>
public static class DatabaseProviders
{
    public const string PostgreSqlProvider = "PostgreSQL";
    public const string SqlServerProvider = "SQL Server";
    public const string MySqlProvider = "MySQL";
    public const string SqliteProvider = "SQLite";
    public const string OracleProvider = "Oracle";
    public const string InMemoryProvider = "In-Memory";
    public const string UnknownProvider = "Unknown";

    public static string Detect(string? providerName)
    {
        return providerName switch
        {
            not null when providerName.Contains("npgsql") => PostgreSqlProvider,
            not null when providerName.Contains("sqlserver") => SqlServerProvider,
            not null when providerName.Contains("sqlite") => SqliteProvider,
            not null when providerName.Contains("mysql") => MySqlProvider,
            not null when providerName.Contains("oracle") => OracleProvider,
            not null when providerName.Contains("inmemory") => InMemoryProvider,
            _ => providerName ?? UnknownProvider
        };
    }
}

/// <summary>
/// Constants for JSON formatting patterns used in repository implementations.
/// </summary>
public static class JsonPatterns
{
    public const string KeyValuePattern = "\"{0}\":\"{1}\"";
}

/// <summary>
/// Constants for GraphQL operations used in repository implementations.
/// </summary>
public static class GraphQlOperators
{
    public const string Equal = "eq";
    public const string Contains = "contains";
    public const string StartsWith = "startsWith";
    public const string EndsWith = "endsWith";
}

/// <summary>
/// Constants for JSON path operations used in repository implementations.
/// </summary>
public static class JsonPathOperations
{
    public const string Equal = "equal";
    public const string Contains = "contains";
    public const string Exists = "exists";
}
