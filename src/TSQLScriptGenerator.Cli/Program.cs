using TSQLScriptGenerator;

namespace TSQLScriptGenerator.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help" or "/?")
        {
            Console.WriteLine(HelpText);
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0].ToLowerInvariant();
        if (command is "-h" or "--help")
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        if (command is not ("databases" or "inserts" or "proc" or "cursor" or "csharp"))
        {
            throw new ArgumentException($"Unknown command '{args[0]}'. Run tsqlscript --help.");
        }

        var options = ParseOptions(args.Skip(1).ToArray());
        if (options.ContainsKey("help"))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        var connection = BuildConnection(options);
        if (command != "databases" && string.IsNullOrWhiteSpace(connection.Database))
        {
            throw new ArgumentException("A database is required (--database or -d).");
        }

        switch (command)
        {
            case "databases":
                foreach (var name in SqlScriptExporter.ListDatabases(connection))
                {
                    Console.WriteLine(name);
                }

                return 0;
            case "inserts":
                Require(options, "table", "A destination table is required (--table).");
                new SqlScriptExporter(connection).ExportInserts(
                    RequireOutput(options),
                    RequireQuery(options),
                    options["table"]!,
                    ifNotExists: Flag(options, "if-not-exists"),
                    overwrite: Flag(options, "force"));
                return 0;
            case "proc":
                Require(options, "procedure", "A procedure name is required (--procedure).");
                new SqlScriptExporter(connection).ExportProcedureCalls(
                    RequireOutput(options),
                    RequireQuery(options),
                    options["procedure"]!,
                    overwrite: Flag(options, "force"));
                return 0;
            case "cursor":
                new SqlScriptExporter(connection).ExportCursor(
                    RequireOutput(options),
                    RequireQuery(options),
                    options.TryGetValue("table", out var label) ? label ?? "query" : "query",
                    overwrite: Flag(options, "force"));
                return 0;
            case "csharp":
                new SqlScriptExporter(connection).ExportCSharp(
                    RequireOutput(options),
                    RequireQuery(options),
                    overwrite: Flag(options, "force"));
                return 0;
            default:
                throw new ArgumentException($"Unknown command '{args[0]}'. Run tsqlscript --help.");
        }
    }

    private static SqlConnectionOptions BuildConnection(Dictionary<string, string?> options)
    {
        var server = Require(options, "server", "A SQL Server instance is required (--server or -S).");
        var database = options.TryGetValue("database", out var databaseName) ? databaseName : null;
        var user = options.TryGetValue("user", out var userId) ? userId : null;
        string? password = null;
        if (!string.IsNullOrWhiteSpace(user))
        {
            if (options.TryGetValue("password", out var supplied) && !string.IsNullOrEmpty(supplied))
            {
                password = supplied;
            }
            else
            {
                password = Environment.GetEnvironmentVariable("SQLCMDPASSWORD");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("SQL authentication needs a password (--password or the SQLCMDPASSWORD environment variable).");
            }
        }
        else if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Warning: integrated security on Linux uses Kerberos. If the connection fails, pass --user and --password for SQL authentication.");
        }

        var timeout = 500;
        if (options.TryGetValue("timeout", out var timeoutText) && !string.IsNullOrWhiteSpace(timeoutText))
        {
            if (!int.TryParse(timeoutText, out timeout) || timeout < 0)
            {
                throw new ArgumentException("Timeout must be a non-negative number of seconds.");
            }
        }

        return new SqlConnectionOptions
        {
            Server = server,
            Database = database,
            TimeoutSeconds = timeout,
            Encrypt = Flag(options, "encrypt"),
            TrustServerCertificate = !options.TryGetValue("trust-server-certificate", out var trust)
                || !string.Equals(trust, "false", StringComparison.OrdinalIgnoreCase),
            UserId = string.IsNullOrWhiteSpace(user) ? null : user,
            Password = password,
        };
    }

    private static string RequireQuery(Dictionary<string, string?> options)
    {
        var hasQuery = options.TryGetValue("query", out var query) && !string.IsNullOrWhiteSpace(query);
        var hasFile = options.TryGetValue("query-file", out var file) && !string.IsNullOrWhiteSpace(file);
        if (hasQuery && hasFile)
        {
            throw new ArgumentException("Pass either --query or --query-file, not both.");
        }

        if (hasFile)
        {
            return File.ReadAllText(file!);
        }

        if (hasQuery)
        {
            return query!;
        }

        throw new ArgumentException("A query is required (--query or --query-file).");
    }

    private static string RequireOutput(Dictionary<string, string?> options)
    {
        return Require(options, "output", "An output file is required (--output or -o). Existing files are left alone unless you pass --force.");
    }

    private static string Require(Dictionary<string, string?> options, string name, string message)
    {
        if (!options.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value;
    }

    private static bool Flag(Dictionary<string, string?> options, string name)
    {
        return options.TryGetValue(name, out var value)
            && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string?> ParseOptions(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith('-'))
            {
                throw new ArgumentException($"Unexpected argument '{arg}'.");
            }

            var name = arg.TrimStart('-');
            string? value;
            var equals = name.IndexOf('=');
            if (equals >= 0)
            {
                value = name[(equals + 1)..];
                name = name[..equals];
            }
            else if (IsFlag(Canonical(name)))
            {
                if (i + 1 < args.Length && IsBoolean(args[i + 1]))
                {
                    value = args[++i];
                }
                else
                {
                    value = "true";
                }
            }
            else
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
                {
                    throw new ArgumentException($"Missing value for --{name}.");
                }

                value = args[++i];
            }

            result[Canonical(name)] = value;
        }

        return result;
    }

    private static bool IsFlag(string canonical) => canonical is "if-not-exists" or "force" or "encrypt" or "trust-server-certificate" or "help";

    private static bool IsBoolean(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("false", StringComparison.OrdinalIgnoreCase);

    private static string Canonical(string name) => name.ToLowerInvariant() switch
    {
        "s" => "server",
        "d" => "database",
        "q" => "query",
        "i" => "query-file",
        "o" => "output",
        "t" => "timeout",
        "u" => "user",
        "p" => "password",
        "h" or "?" => "help",
        _ => name.ToLowerInvariant(),
    };

    private const string HelpText = """
        tsqlscript generates T-SQL scripts from a SELECT.

        Usage:
          tsqlscript inserts  --server SERVER --database DB --query "SELECT ..." --table dbo.Target --output out.sql
          tsqlscript proc     --server SERVER --database DB --query-file query.sql --procedure dbo.usp_Load --output out.sql
          tsqlscript cursor   --server SERVER --database DB --query-file query.sql --output cursor.sql
          tsqlscript csharp   --server SERVER --database DB --query-file query.sql --output reader.cs
          tsqlscript databases --server SERVER

        inserts writes one INSERT ... GO batch per row, preceded by CREATE TABLE if the destination is missing.
        proc writes EXEC procedure calls, one per row. Column names become parameter names.
        cursor writes a T-SQL cursor scaffold over the query.
        csharp writes a Microsoft.Data.SqlClient reader fragment.

        Connection:
          --server, -S          Instance name. Required.
          --database, -d        Database. Required except for the databases command.
          --timeout, -t         Connection and command timeout in seconds. Default 500.
          --user, -U            SQL login. Omit to use integrated security.
          --password, -P        SQL password. If omitted, SQLCMDPASSWORD is read.
          --encrypt             Require TLS. Default is off, matching System.Data.SqlClient on .NET Framework.
          --trust-server-certificate true|false
                                Default true, so an internal instance with a self-signed certificate still connects when you opt into --encrypt.

        Query and output:
          --query, -Q           SELECT text.
          --query-file, -i      File containing the SELECT.
          --output, -o          Script path. Refuses to overwrite an existing file unless --force is set.
          --table               Destination table for inserts. Also used as the cursor label in error text.
          --procedure           Procedure name for proc.
          --if-not-exists       Wrap each insert in IF NOT EXISTS on the primary key. The query must expose that key.
          --force               Overwrite the output file.

        Examples:
          tsqlscript inserts -S localhost -d AdventureWorks -Q "SELECT TOP (10) BusinessEntityID, FirstName, LastName FROM Person.Person" --table dbo.PersonCopy -o person.sql
          tsqlscript inserts -S localhost -d AdventureWorks -i currency.sql --table Sales.Currency -o currency.sql --if-not-exists
          tsqlscript proc -S localhost -d AdventureWorks -i people.sql --procedure dbo.usp_LoadPerson -o load.sql
        """;
}
