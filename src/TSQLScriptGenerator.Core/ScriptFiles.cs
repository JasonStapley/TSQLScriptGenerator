using System.Text;

namespace TSQLScriptGenerator;

public static class ScriptFiles
{
    /// <summary>
    /// Writes a script as UTF-8 with a BOM. SSMS and sqlcmd detect that encoding reliably.
    /// The caller supplies the line endings (the generator uses CRLF).
    /// </summary>
    public static StreamWriter CreateText(string path, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!overwrite && File.Exists(path))
        {
            throw new IOException($"'{path}' already exists.");
        }

        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        var stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read);
        var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.NewLine = "\r\n";
        return writer;
    }
}
