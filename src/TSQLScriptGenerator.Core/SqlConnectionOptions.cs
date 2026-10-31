using Microsoft.Data.SqlClient;

namespace TSQLScriptGenerator;

/// <summary>
/// Connection settings for the exporter. Integrated security is the default, matching the original desktop app.
/// Encryption defaults to optional so existing on-prem instances keep working after the move to Microsoft.Data.SqlClient,
/// which otherwise encrypts by default.
/// </summary>
public sealed record SqlConnectionOptions
{
    public required string Server { get; init; }

    public string? Database { get; init; }

    /// <summary>Used for both the connection timeout and the command timeout, as the original form did.</summary>
    public int TimeoutSeconds { get; init; } = 500;

    public bool Encrypt { get; init; }

    public bool TrustServerCertificate { get; init; } = true;

    public string? UserId { get; init; }

    public string? Password { get; init; }
}

public static class SqlConnectionStrings
{
    public const string ApplicationName = "TSQLScriptGenerator";

    public static string Build(SqlConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Server))
        {
            throw new ArgumentException("A SQL Server instance is required.", nameof(options));
        }

        if (options.TimeoutSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout cannot be negative.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.Server.Trim(),
            ConnectTimeout = options.TimeoutSeconds,
            ApplicationName = ApplicationName,
            Encrypt = options.Encrypt ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,
            TrustServerCertificate = options.TrustServerCertificate,
        };

        if (!string.IsNullOrWhiteSpace(options.Database))
        {
            builder.InitialCatalog = options.Database.Trim();
        }

        if (!string.IsNullOrWhiteSpace(options.UserId))
        {
            builder.IntegratedSecurity = false;
            builder.UserID = options.UserId.Trim();
            builder.Password = options.Password ?? "";
        }
        else
        {
            builder.IntegratedSecurity = true;
        }

        return builder.ConnectionString;
    }
}
