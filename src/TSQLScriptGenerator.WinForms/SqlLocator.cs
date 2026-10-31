using System.Runtime.InteropServices;
using System.Text;

namespace TSQLScriptGenerator;

/// <summary>
/// Lists SQL Server instances through the Windows ODBC driver manager.
/// This stays in the desktop app: it depends on odbc32.dll and is not used by the cross-platform CLI.
/// </summary>
public static class SqlLocator
{
    [DllImport("odbc32.dll")]
    private static extern short SQLAllocHandle(short hType, IntPtr inputHandle, out IntPtr outputHandle);

    [DllImport("odbc32.dll")]
    private static extern short SQLSetEnvAttr(IntPtr henv, int attribute, IntPtr valuePtr, int strLength);

    [DllImport("odbc32.dll")]
    private static extern short SQLFreeHandle(short hType, IntPtr handle);

    [DllImport("odbc32.dll", CharSet = CharSet.Ansi)]
    private static extern short SQLBrowseConnect(
        IntPtr hconn,
        StringBuilder inString,
        short inStringLength,
        StringBuilder outString,
        short outStringLength,
        out short outLengthNeeded);

    private const short SQL_HANDLE_ENV = 1;
    private const short SQL_HANDLE_DBC = 2;
    private const int SQL_ATTR_ODBC_VERSION = 200;
    private const int SQL_OV_ODBC3 = 3;
    private const short SQL_SUCCESS = 0;
    private const short SQL_NEED_DATA = 99;
    private const short DEFAULT_RESULT_SIZE = 1024;
    private const string SQL_DRIVER_STR = "DRIVER=SQL SERVER";

    public static string[] GetServers()
    {
        string[] retval = null;
        var txt = string.Empty;
        var henv = IntPtr.Zero;
        var hconn = IntPtr.Zero;
        var inString = new StringBuilder(SQL_DRIVER_STR);
        var outString = new StringBuilder(DEFAULT_RESULT_SIZE);
        var inStringLength = (short)inString.Length;
        short lenNeeded = 0;

        try
        {
            if (SQL_SUCCESS == SQLAllocHandle(SQL_HANDLE_ENV, henv, out henv))
            {
                if (SQL_SUCCESS == SQLSetEnvAttr(henv, SQL_ATTR_ODBC_VERSION, (IntPtr)SQL_OV_ODBC3, 0))
                {
                    if (SQL_SUCCESS == SQLAllocHandle(SQL_HANDLE_DBC, henv, out hconn))
                    {
                        if (SQL_NEED_DATA == SQLBrowseConnect(hconn, inString, inStringLength, outString, DEFAULT_RESULT_SIZE, out lenNeeded))
                        {
                            if (DEFAULT_RESULT_SIZE < lenNeeded)
                            {
                                outString.Capacity = lenNeeded;
                                if (SQL_NEED_DATA != SQLBrowseConnect(hconn, inString, inStringLength, outString, lenNeeded, out lenNeeded))
                                {
                                    throw new ApplicationException("Unabled to aquire SQL Servers from ODBC driver.");
                                }
                            }

                            txt = outString.ToString();
                            var start = txt.IndexOf('{') + 1;
                            var len = txt.IndexOf('}') - start;
                            if (start > 0 && len > 0)
                            {
                                txt = txt.Substring(start, len);
                            }
                            else
                            {
                                txt = string.Empty;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Acquire SQL Server List Error");
            txt = string.Empty;
        }
        finally
        {
            if (hconn != IntPtr.Zero)
            {
                SQLFreeHandle(SQL_HANDLE_DBC, hconn);
            }

            if (henv != IntPtr.Zero)
            {
                SQLFreeHandle(SQL_HANDLE_ENV, henv);
            }
        }

        if (txt.Length > 0)
        {
            retval = txt.Split(',');
        }

        return retval;
    }
}
