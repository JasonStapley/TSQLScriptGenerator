# TSQLScriptGenerator

Generates T-SQL `INSERT` statements or stored procedure calls from a `SELECT`. Point it at a SQL Server instance, give it a query and a destination name, and it writes a script you can run in SSMS or sqlcmd.

The .NET Framework 4.5 WinForms app is now an SDK-style .NET 8 solution:

| Project | Target | What it is |
| --- | --- | --- |
| `src/TSQLScriptGenerator.Core` | `net8.0` | Script generation and `Microsoft.Data.SqlClient` export |
| `src/TSQLScriptGenerator.Cli` | `net8.0` | Cross-platform `tsqlscript` command |
| `src/TSQLScriptGenerator.WinForms` | `net8.0-windows` | The original desktop form |
| `tests/TSQLScriptGenerator.Tests` | `net8.0` | xUnit tests for literals, scripts, and connection strings |

There is no `packages.config`. Packages are `PackageReference`s. SQL access uses `Microsoft.Data.SqlClient` 7.1.1.

## Build and test

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then from the repository root:

```bash
dotnet test TSQLScriptGenerator.sln
```

That restores, builds, and runs the tests. `global.json` stays on the 8.0 SDK band.

The WinForms project targets `net8.0-windows`. It builds on Windows. It also builds on other operating systems when `EnableWindowsTargeting` is set, which this project does, but it only runs on Windows.

GitHub Actions builds and tests on Ubuntu and Windows for every push and pull request (`.github/workflows/build.yml`).

## Command line

```bash
dotnet run --project src/TSQLScriptGenerator.Cli -- --help
```

After `dotnet build`, the executable is `src/TSQLScriptGenerator.Cli/bin/Debug/net8.0/tsqlscript` (or `tsqlscript.exe` on Windows).

```bash
# One INSERT ... GO per row, plus CREATE TABLE if the destination is missing.
tsqlscript inserts \
  --server localhost \
  --database AdventureWorks \
  --query "SELECT TOP (10) BusinessEntityID, FirstName, LastName FROM Person.Person" \
  --table dbo.PersonCopy \
  --output person.sql

# Same thing, but skip a row when its primary key is already in the destination.
# The SELECT must include the key columns.
tsqlscript inserts \
  --server localhost \
  --database AdventureWorks \
  --query-file currency.sql \
  --table Sales.Currency \
  --output currency.sql \
  --if-not-exists

# EXEC dbo.usp_LoadPerson @BusinessEntityID=..., @FirstName=...
tsqlscript proc \
  --server localhost \
  --database AdventureWorks \
  --query-file people.sql \
  --procedure dbo.usp_LoadPerson \
  --output load.sql

tsqlscript cursor --server localhost --database AdventureWorks --query-file people.sql --output cursor.sql
tsqlscript csharp --server localhost --database AdventureWorks --query-file people.sql --output reader.cs
tsqlscript databases --server localhost
```

Short options follow sqlcmd where they overlap: `-S` server, `-d` database, `-Q` query, `-i` query file, `-o` output, `-t` timeout, `-U` login, `-P` password.

The query is executed as written. Prefer a `SELECT`. An existing output file is left alone unless you pass `--force`.

Integrated security is the default. On Linux that means Kerberos; if that is not set up, pass a SQL login. The password can be `--password` or the `SQLCMDPASSWORD` environment variable. A generated C# fragment never contains the password you passed in; it writes `YOUR_PASSWORD` instead.

`--encrypt` turns on TLS. It is off by default so existing on-prem instances behave as they did with `System.Data.SqlClient` on .NET Framework, which did not encrypt. Microsoft.Data.SqlClient would otherwise default to encrypting, and a lot of internal instances do not have a certificate the client trusts. With the default settings, `--trust-server-certificate` stays true, so turning on `--encrypt` still accepts a self-signed certificate. Pass `--trust-server-certificate false` when you want the client to validate the certificate chain.

## Desktop app

On Windows:

```bash
dotnet run --project src/TSQLScriptGenerator.WinForms
```

The form is the same tool: server, database, timeout, query, and an output mode (inserts, `IF NOT EXISTS` inserts, procedure calls, a C# reader fragment, or a cursor scaffold). The server list still comes from the Windows ODBC driver manager (`odbc32.dll`, driver `SQL SERVER`) and only works on Windows. The database list is a `SELECT [name] FROM sys.databases` against `master`.

Publish a framework-dependent build with:

```bash
dotnet publish src/TSQLScriptGenerator.WinForms/TSQLScriptGenerator.WinForms.csproj -c Release -r win-x64 --self-contained false
```

The machine that runs it needs the .NET 8 desktop runtime. `setup/setup.iss` is an Inno Setup script aimed at that publish folder. The `setup.exe` in the repository root is still the old .NET Framework 4.5 installer and has not been rebuilt.

## What a script looks like

An insert script for `Id` (int, primary key) and `Note` (nvarchar) looks like this. Line endings in the file are CRLF, and the file is UTF-8 with a BOM so SSMS detects it.

```sql
IF OBJECT_ID(N'[dbo].[Orders]') IS NULL
BEGIN
CREATE TABLE [dbo].[Orders]([Id] int NOT NULL,[Note] nvarchar(100) NULL
);
ALTER TABLE [dbo].[Orders]
ADD PRIMARY KEY
([Id]
);
END
INSERT INTO [dbo].[Orders]([Id],[Note])
VALUES(1,N'O''Brien')
GO
INSERT INTO [dbo].[Orders]([Id],[Note])
VALUES(2,NULL)
GO
```

`IF NOT EXISTS` wraps each insert. The old script glued `AND` onto the previous literal (`1AND [Code]=...`), so the predicate was not valid T-SQL. There is a space around `AND` now:

```sql
IF NOT EXISTS(SELECT 1 FROM [dbo].[Orders] WHERE [Id]=1 AND [Code]=N'A')
BEGIN
	INSERT INTO [dbo].[Orders]([Id],[Code])
	VALUES(1,N'A')
END

GO
```

Procedure calls:

```sql
EXEC [dbo].[usp_Load] @[Id]=1,@[Name]=N'O''Brien'
GO
```

## How values are written

Formatting does not follow the machine locale. A decimal point is always `.`.

- `NULL` and `DBNull` become `NULL`.
- `bit` becomes `1` or `0`.
- Integers, `decimal`, `numeric`, `money`, and `smallmoney` are invariant digits. Money keeps four decimal places (`1.2000`). `decimal` and `numeric` use the column scale.
- `float` and `real` use round-trip formatting (`G17` / `G9`). Some values are long (`0.1` is not a tidy `0.1`). NaN and infinity are rejected because SQL Server has no literal for them.
- `date` is `'yyyy-MM-dd'`. `datetime`, `smalldatetime`, and `datetime2` use `yyyy-MM-ddTHH:mm:ss` with the `T` separator, which is not affected by `SET DATEFORMAT` or `SET LANGUAGE`. Fractional digits follow the type (`datetime` is `.fff`, `datetime2` follows its scale). `time` and `datetimeoffset` keep their scale, and `datetimeoffset` includes the offset.
- `uniqueidentifier` is a lowercase canonical string in quotes.
- `binary`, `varbinary`, `image`, `timestamp`, and `rowversion` are `0x` plus uppercase hex. An empty value is `0x`, not `NULL`. `NULL` is still `NULL`.
- `nvarchar`, `nchar`, `ntext`, `sysname`, and `xml` use `N''`. Quotes inside a string are doubled. A `varchar`, `char`, or `text` value that contains a non-ASCII character is also written as `N''` so the database code page does not reinterpret it.
- A NUL character inside a string is split out with `CHAR(0)` or `NCHAR(0)`. A NUL left inside the literal can truncate the script in some editors.
- Identifiers are bracketed per part: `dbo.Orders` becomes `[dbo].[Orders]`, and a `]` inside a name is doubled. `CREATE TABLE`, `INSERT`, `EXEC`, and `OBJECT_ID` all use that form.
- `varchar(max)`, `nvarchar(max)`, and `varbinary(max)` are written as `(max)` instead of the 2-billion character length the driver reports. `decimal` and `numeric` include precision and scale. `datetime2`, `datetimeoffset`, and `time` include their scale.

`CREATE TABLE` is emitted only for the insert modes, and only inside `IF OBJECT_ID(...) IS NULL`. When the result metadata marks a column as a key, a primary key constraint is added. The .NET Framework build compared that flag to the string `"true"`, so the constraint was never emitted. It is emitted now.

Rows are still one `INSERT` plus `GO` each, not a multi-row `VALUES` clause.

## Things worth checking on a real instance

This environment has no SQL Server, so the tests cover the script text, not a live connection.

- Run `inserts` against a table that has strings with quotes, `nvarchar` data, `NULL`s, `datetime2`, `varbinary`, and a `decimal` column. Compare a couple of rows with the old tool if you still have it.
- Run `inserts --if-not-exists` on `SELECT *` from a table with a primary key, and on a query that does not include the key. The second one should fail with the same "no primary keys" message as before.
- Confirm `Encrypt=Optional` is what you want. Pass `--encrypt` for instances that require TLS.
- On Windows, open the desktop app and drop down the server list. That path still depends on the legacy ODBC "SQL Server" driver.
- The form icon was stored in the `.resx` with `BinaryFormatter`, which .NET 8 will not load. The form no longer sets an icon.
- `setup.exe` at the repository root is the old installer.
