using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Security.Permissions;
using System.Windows.Forms;

namespace TSQLScriptGenerator
{
    class GenerateTSQL
    {
        /// <summary>
        /// Export MsSQL To TSQL file
        /// </summary>
        /// <param name="sqlserver">The name of the SQL Server</param>
        /// <param name="sqldatabase">The name of the SQL Database</param>
        /// <param name="file">The file name of the SQL Lite database</param>
        /// <param name="query">The SQL query to be run against the database</param>
        /// <param name="table">The Name of the table we want to export to</param>
        public static void ExportMsSQLToScript(string sqlserver, string sqldatabase, string file, string query, string table, string timeout)
        {
            DBDataType dt = new DBDataType();
            string sqltableschema = "";
            string sqlinsertstatement = "";


            using (SqlConnection SqlConn = new SqlConnection(string.Format("server={0};integrated security=SSPI;Connection Timeout={1}", sqlserver, timeout)))
            {
                SqlConn.Open();
                SqlConn.ChangeDatabase(sqldatabase);
                
                using (SqlCommand SqlCmd = new SqlCommand(query, SqlConn))
                {
                    SqlCmd.CommandType = System.Data.CommandType.Text;
                    // Set timeout
                    SqlCmd.CommandTimeout = Int32.Parse(timeout);
                    var reader = SqlCmd.ExecuteReader() as SqlDataReader;
                    // If there are rows
                    if (reader.HasRows == true)
                    {
                        Console.Out.WriteLine(table);

                        DataTable datadatabase = reader.GetSchemaTable();
                        // Create schema if needed
                        sqltableschema = GenerateTSQL.CreateSQLTableSchema(datadatabase, table);
                        //Console.Out.WriteLine(sqltableschema);
                        sqlinsertstatement = GenerateTSQL.CreateSQLInsertHeader(datadatabase, table);
                        //Console.Out.WriteLine(sqlinsertstatement);
                        using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (TextWriter tr = new StreamWriter(fs))
                            {
                                tr.WriteLine(sqltableschema);
                                // Create insert statements
                                // Rows
                                while (reader.Read())
                                {
                                    tr.WriteLine(sqlinsertstatement);
                                    tr.Write("VALUES(");
                                    // Columns
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (i > 0)
                                        {
                                            tr.Write(",");
                                        }
                                        // Check for nulls
                                        if (reader.IsDBNull(i))
                                        {
                                            tr.Write("NULL");
                                        }
                                        else
                                        {
                                            if (dt.Escapable((string)datadatabase.Rows[i]["DataTypeName"]))
                                            {
                                                if (datadatabase.Rows[i]["DataTypeName"].Equals("datetime") == true)
                                                {
                                                    DateTime datetime = reader.GetDateTime(i);
                                                    string s = string.Format("{0:yyyy-MM-dd HH:mm:ss}", datetime);
                                                    tr.Write("'" + s + "'");
                                                }
                                                else if (datadatabase.Rows[i]["DataTypeName"].Equals("smalldatetime") == true)
                                                {
                                                    DateTime datetime = reader.GetDateTime(i);
                                                    string s = string.Format("{0:yyyy-MM-dd HH:mm:ss}", datetime);
                                                    tr.Write("'" + s + "'");
                                                }
                                                else if (datadatabase.Rows[i]["DataTypeName"].Equals("date") == true)
                                                {
                                                    DateTime datetime = reader.GetDateTime(i);
                                                    string s = string.Format("{0:yyyy-MM-dd}", datetime);
                                                    tr.Write("'" + s + "'");
                                                }
                                                else if (datadatabase.Rows[i]["DataTypeName"].Equals("nvarchar") == true)
                                                {
                                                    tr.Write("N'" + reader.GetSqlString(i) + "'");                                                    
                                                }
                                                else
                                                {
                                                    tr.Write("'" + reader[i].ToString().Replace("'", "''") + "'");
                                                }
                                            }
                                            else
                                            {
                                                if (datadatabase.Rows[i]["DataTypeName"].Equals("bit") == true)
                                                {
                                                    if ((bool)reader[i] == false)
                                                    {
                                                        tr.Write("0");
                                                    }
                                                    else
                                                    {
                                                        tr.Write("1");
                                                    }
                                                }
                                                else if (datadatabase.Rows[i]["DataTypeName"].Equals("varbinary") == true)
                                                {                                                    
                                                    tr.Write("0x");
                                                    tr.Write(BitConverter.ToString((byte[])reader[i]).Replace("-", ""));
                                                }
                                                else
                                                {
                                                    tr.Write(reader[i]);
                                                }
                                            }
                                        }
                                    }
                                    tr.WriteLine(")\nGO");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("No rows for extract Query {0}", table));                        
                    }
                }
            }
        }

/// <summary>
/// Get the list of primary keys
/// </summary>
/// <param name="sqlserver"></param>
/// <param name="sqldatabase"></param>
/// <param name="query"></param>
/// <param name="timeout"></param>
/// <returns></returns>
        public static List<string> GetPrimaryKeys(string sqlserver, string sqldatabase, string query,  string timeout)
        {
            List<string> retval = new List<string>();
            SqlDataAdapter da = new SqlDataAdapter(query, string.Format("server={0};Database={1};integrated security=SSPI;Connection Timeout={2}", sqlserver, sqldatabase, timeout));

            da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            DataTable datatable = new DataTable();
            da.Fill(datatable);
            DataTable schemaTable;

            using (DataTableReader reader = new DataTableReader(datatable))
            {
                schemaTable = reader.GetSchemaTable();
                foreach (DataRow row in schemaTable.Rows)
                {
                    foreach (DataColumn col in schemaTable.Columns)
                    {
                        if (col.ColumnName.Equals("IsKey") && ((bool)row[col.Ordinal]==true))
                        {
                            retval.Add(row["ColumnName"].ToString());
                        }
                    }
                }                
            }
            return retval;
        }


        public static DataTable GetQuerySchema(string sqlserver, string sqldatabase, string query, string timeout)
        {
            DataTable retval;
            SqlDataAdapter da = new SqlDataAdapter(query, string.Format("server={0};Database={1};integrated security=SSPI;Connection Timeout={2}", sqlserver, sqldatabase, timeout));

            da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            DataTable datatable = new DataTable();
            da.Fill(datatable);            

            using (DataTableReader reader = new DataTableReader(datatable))
            {
                retval = reader.GetSchemaTable();                
            }
            return retval;
        }


        public static string RenderFieldValue(int i, SqlDataReader reader, DataTable database)
        {
            DBDataType dt = new DBDataType();
            // Check for nulls
            if (reader.IsDBNull(i))
            {
                return "NULL" ;
            }
            else
            {
                if (dt.Escapable((string)database.Rows[i]["DataTypeName"]))
                {
                    if (database.Rows[i]["DataTypeName"].Equals("datetime") == true)
                    {
                        DateTime datetime = reader.GetDateTime(i);
                        string s = string.Format("{0:yyyy-MM-dd HH:mm:ss}", datetime);
                        return "'" + s + "'";
                    }
                    else if (database.Rows[i]["DataTypeName"].Equals("smalldatetime") == true)
                    {
                        DateTime datetime = reader.GetDateTime(i);
                        string s = string.Format("{0:yyyy-MM-dd HH:mm:ss}", datetime);
                        return "'" + s + "'";
                    }
                    else if (database.Rows[i]["DataTypeName"].Equals("date") == true)
                    {
                        DateTime datetime = reader.GetDateTime(i);
                        string s = string.Format("{0:yyyy-MM-dd}", datetime);
                        return "'" + s + "'";
                    }
                    else if (database.Rows[i]["DataTypeName"].Equals("nvarchar") == true)
                    {
                        return "N'" + (string)reader.GetSqlString(i) + "'";
                    }
                    else
                    {
                        return "'" + reader[i].ToString().Replace("'", "''") + "'";
                    }
                }
                else
                {
                    if (database.Rows[i]["DataTypeName"].Equals("bit") == true)
                    {
                        if ((bool)reader[i] == false)
                        {
                            return "0";
                        }
                        else
                        {
                            return "1";
                        }
                    }
                    else if (database.Rows[i]["DataTypeName"].Equals("varbinary") == true)
                    {
                        return "0x" + BitConverter.ToString((byte[])reader[i]).Replace("-", "");
                    }
                    else
                    {
                        return reader[i].ToString();
                    }
                }
            }
        }

        public static void ExportMsSQLToScriptWithIfExists(string sqlserver, string sqldatabase, string file, string query, string table, string timeout)
        {            
            string sqltableschema = "";
            string sqlinsertstatement = "";

            var primarykeys = new List<string>();



             primarykeys = GetPrimaryKeys(sqlserver, sqldatabase, query, timeout);

             if (primarykeys.Count == 0)
             {
                 throw new Exception("No Primary keys set in the source table so I can't generate a if exists");
             }
        
            using (SqlConnection SqlConn = new SqlConnection(string.Format("server={0};integrated security=SSPI;Connection Timeout={1}", sqlserver, timeout)))
            {
                SqlConn.Open();
                SqlConn.ChangeDatabase(sqldatabase);

                using (SqlCommand SqlCmd = new SqlCommand(query, SqlConn))
                {
                    SqlCmd.CommandType = System.Data.CommandType.Text;
                    // Set timeout
                    SqlCmd.CommandTimeout = Int32.Parse(timeout);
                    var reader = SqlCmd.ExecuteReader() as SqlDataReader;
                    // If there are rows
                    if (reader.HasRows == true)
                    {
                        Console.Out.WriteLine(table);

                        DataTable database = reader.GetSchemaTable();
                        // Create schema if needed
                        sqltableschema = GenerateTSQL.CreateSQLTableSchema(database, table);
                        //Console.Out.WriteLine(sqltableschema);
                        sqlinsertstatement = GenerateTSQL.CreateSQLInsertHeader(database, table);
                        //Console.Out.WriteLine(sqlinsertstatement);
                        using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (TextWriter tr = new StreamWriter(fs))
                            {
                                tr.WriteLine(sqltableschema);
                                // Create insert statements
                                // Rows
                                while (reader.Read())
                                {
                                    bool firstprimarykey = true;
                                    // Add if exists statement
                                    tr.Write("\nIF NOT EXISTS(SELECT 1 FROM [" + table + "] WHERE ");

                                    foreach (string primarykey in primarykeys)
                                    {
                                        if (firstprimarykey == true)
                                        {
                                            tr.Write("[" + primarykey + "]=");

                                            tr.Write(GenerateTSQL.RenderFieldValue(reader.GetOrdinal(primarykey), reader, database));
                                            firstprimarykey = false;
                                        }
                                        else
                                        {
                                            tr.Write("AND [" + primarykey + "]=");
                                            tr.Write(GenerateTSQL.RenderFieldValue(reader.GetOrdinal(primarykey), reader, database));
                                        }
                                    }

                                    tr.Write(")");
                                    tr.Write("\nBEGIN\n");

                                    tr.WriteLine("\t" + sqlinsertstatement);
                                    tr.Write("\tVALUES(");
                                    // Columns
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (i > 0)
                                        {
                                            tr.Write(",");                                            
                                        }
                                        tr.Write(GenerateTSQL.RenderFieldValue(i, reader, database));
                                    }
                                    tr.Write(")");
                                    tr.WriteLine("\nEND");
                                    tr.WriteLine("\nGO");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("No rows for extract Query {0}", table));
                    }
                }
            }
        }


        public static void ExportCSharpCall(string sqlserver, string sqldatabase, string file, string query, string table, string timeout)
        {

            DataTable schematable = GenerateTSQL.GetQuerySchema(sqlserver, sqldatabase, query, timeout);

            using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                using (TextWriter tr = new StreamWriter(fs))
                {
                    tr.WriteLine("// Code fragment ");
                    tr.WriteLine("// using System.Data.SqlClient;");
                    tr.WriteLine("// using System.Data;");

                    tr.WriteLine("\nusing (SqlConnection SqlConn = new SqlConnection(string.Format(\"server=THESERVERNAME;integrated security=SSPI;Connection Timeout=CONNECTIONTIMEOUT\", sqlserver, timeout)))");
                    tr.WriteLine(" {");
                    tr.WriteLine(" SqlConn.Open();");
                    tr.WriteLine(" SqlConn.ChangeDatabase(\"THEDATABASE\");");

                    tr.WriteLine(" using (SqlCommand SqlCmd = new SqlCommand(\"" + query + "\", SqlConn)) ");
                    tr.WriteLine("      {");
                    tr.WriteLine("         SqlCmd.CommandType = System.Data.CommandType.Text; ");
                    tr.WriteLine("// Set timeout");
                    tr.WriteLine("SqlCmd.CommandTimeout = 50000;");
                    tr.WriteLine("var reader = SqlCmd.ExecuteReader() as SqlDataReader;");
                    tr.WriteLine("         // If there are rows");
                    tr.WriteLine("         if (reader.HasRows == true)");
                    tr.WriteLine("         {");
                    tr.WriteLine(" while (reader.Read()) ");
                    tr.WriteLine("            { ");

                    //Process the columns
                    foreach (DataRow row in schematable.Rows)
                    {
                        tr.WriteLine(row["DataType"] + " " + row["ColumnName"] + " =  reader.reader.Get" + row["DataType"].ToString().Replace("System.", "") + "(" + row["ColumnOrdinal"] + "); ");     
                    }

                    tr.WriteLine("            } ");
                    tr.WriteLine("            } ");
                    tr.WriteLine("            } ");
                    tr.WriteLine("            } ");                    
                }
            }
        }

        public static void ExportCursor(string sqlserver, string sqldatabase, string file, string query, string table, string timeout)
        {
            DBDataType dt = new DBDataType();
            using (SqlConnection SqlConn = new SqlConnection(string.Format("server={0};integrated security=SSPI;Connection Timeout={1}", sqlserver, timeout)))
            {
                SqlConn.Open();
                SqlConn.ChangeDatabase(sqldatabase);

                using (SqlCommand SqlCmd = new SqlCommand(query, SqlConn))
                {
                    SqlCmd.CommandType = System.Data.CommandType.Text;
                    // Set timeout
                    SqlCmd.CommandTimeout = Int32.Parse(timeout);
                    var reader = SqlCmd.ExecuteReader() as SqlDataReader;
                    // If there are rows
                    if (reader.HasRows == true)
                    {
                        DataTable database = reader.GetSchemaTable();

                        using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (TextWriter tr = new StreamWriter(fs))
                            {                                
                                for (int i = 0; i < database.Rows.Count; i++)
                                {                                   
                                    // Column name
                                    tr.Write("\nDECLARE @" + database.Rows[i]["ColumnName"] + " ");
                                    tr.Write(database.Rows[i]["DataTypeName"] + " ");
                                    if (dt.Sizeable(database.Rows[i]["DataTypeName"].ToString()) == true)
                                    {
                                        tr.Write("(" + database.Rows[i]["ColumnSize"] + ")");
                                    }

                                    tr.Write(" = NULL");                                    
                                }

                                tr.WriteLine("\nDECLARE @TableCursor as CURSOR");
                                tr.WriteLine("SET @TableCursor = CURSOR READ_ONLY FOR SELECT * FROM [dbo].[DocumentTypes]");
                                tr.WriteLine("OPEN @TableCursor");

                                tr.WriteLine("FETCH NEXT FROM @TableCursor INTO ");

                                for (int i = 0; i < database.Rows.Count; i++)
                                {
                                    if (i > 0)
                                    {
                                        tr.Write(",");
                                    }
                                    // Column name
                                    tr.Write("@" + database.Rows[i]["ColumnName"] + " ");                                    
                                }

                                tr.WriteLine("\nWHILE @@FETCH_STATUS = 0");
                                tr.WriteLine("BEGIN");
                                tr.WriteLine("-- Code here");


                                tr.WriteLine("FETCH NEXT FROM @TableCursor INTO ");

                                for (int i = 0; i < database.Rows.Count; i++)
                                {
                                    if (i > 0)
                                    {
                                        tr.Write(",");
                                    }
                                    // Column name
                                    tr.Write("@" + database.Rows[i]["ColumnName"] + " ");                                    
                                }

                                tr.WriteLine("\nEND");

                                tr.WriteLine("CLOSE @TableCursor");
                                tr.WriteLine("DEALLOCATE @TableCursor");                                
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("No rows for cursor {0}", table));
                    }
                }
            }
        }
       
        public static void ExportQueryToStoredProcedureCalls(string sqlserver, string sqldatabase, string file, string query, string table, string timeout)
        {
            string sqltableschema = "";
            string sqlinsertstatement = "";

            using (SqlConnection SqlConn = new SqlConnection(string.Format("server={0};integrated security=SSPI;Connection Timeout={1}", sqlserver, timeout)))
            {
                SqlConn.Open();
                SqlConn.ChangeDatabase(sqldatabase);

                using (SqlCommand SqlCmd = new SqlCommand(query, SqlConn))
                {
                    SqlCmd.CommandType = System.Data.CommandType.Text;
                    // Set timeout
                    SqlCmd.CommandTimeout = Int32.Parse(timeout);
                    var reader = SqlCmd.ExecuteReader() as SqlDataReader;
                    // If there are rows
                    if (reader.HasRows == true)
                    {
                        Console.Out.WriteLine(table);

                        DataTable database = reader.GetSchemaTable();
                        // Create schema if needed
                        sqltableschema = GenerateTSQL.CreateSQLTableSchema(database, table);
                        //Console.Out.WriteLine(sqltableschema);
                        sqlinsertstatement = GenerateTSQL.CreateSQLInsertHeader(database, table);
                        //Console.Out.WriteLine(sqlinsertstatement);
                        using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (TextWriter tr = new StreamWriter(fs))
                            {                                                                                             
                                while (reader.Read())
                                {                                    
                                    tr.Write("\nEXEC [" + table + "] ");                                    
                                    // Columns
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (i > 0)
                                        {
                                            tr.Write(",");
                                        }
                                        tr.Write("@" + ("" + database.Rows[i]["ColumnName"] + "="));
                                        tr.Write(GenerateTSQL.RenderFieldValue(i, reader, database));
                                    }
                                    
                                    tr.WriteLine("\nGO");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("No rows for extract Query {0}", table));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a SQL table schema from a query data set
        /// </summary>
        /// <param name="sqlschema">The data table with the SQL Schema</param>
        /// <returns>Wether or not the column need single quotes arount it</returns>
        public static bool IsEscapable(DataTable sqlschema, string columnname)
        {
            DBDataType dt = new DBDataType();
            bool retval = false;
            // Loop through each column
            for (int i = 0; i < sqlschema.Rows.Count; i++)
            {
                if (sqlschema.Rows[i]["ColumnName"].Equals(columnname))
                {
                    retval = dt.Escapable((string)sqlschema.Rows[i]["DataTypeName"]);
                    break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Does the data reader object have have a primary id we could use?
        /// </summary>
        /// <param name="sqlschema">The dataset schema from a reader object</param>
        /// <returns></returns>
        public static bool HasPrimaryKey(DataTable sqlschema)
        {
            foreach (DataRow row in sqlschema.Rows)
            {
                foreach (DataColumn col in sqlschema.Columns)
                {
                    if (col.ColumnName.Equals("IsKey") && row[col.Ordinal].Equals("True"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Creates a SQL table schema from a query data set
        /// </summary>
        /// <param name="sqlschema">The data table with the SQL Schema</param>
        /// <returns>The SQL Schema as a string</returns>
        public static string CreateSQLTableSchema(DataTable sqlschema, string tablename)
        {
            DBDataType dt = new DBDataType();
            StringBuilder retval = new StringBuilder("IF OBJECT_ID('" + tablename + "') IS NULL\n" +
            "BEGIN\n" +
            "CREATE TABLE ", 10);
            retval.Append(tablename);
            retval.Append("(");
            // Loop through each column
            for (int i = 0; i < sqlschema.Rows.Count; i++)
            {
                //ProviderType
                if (i > 0)
                {
                    retval.Append(",");
                }
                // Column name
                retval.Append("[" + sqlschema.Rows[i]["ColumnName"] + "]");
                retval.Append(" ");
                retval.Append(sqlschema.Rows[i]["DataTypeName"]);
                // If its a varchar the string size
                if (dt.Sizeable(sqlschema.Rows[i]["DataTypeName"].ToString()) == true)
                {
                    retval.Append("(" + sqlschema.Rows[i]["ColumnSize"] + ")");
                }

                if ((bool)sqlschema.Rows[i]["AllowDBNull"] == true)
                {
                    retval.Append(" NULL");
                }
                else
                {
                    retval.Append(" NOT NULL");
                }
            }
            retval.Append("\n);");

            retval.Append("\n" + GenerateTSQL.CreateSQLTablePrimaryKeyContrainst(sqlschema, tablename));
            
            retval.Append("\nEND");

            return retval.ToString();
        }

        /// <summary>
        /// Creates a Primary key constraint
        /// </summary>
        /// <param name="sqlschema">The data table with the SQL Schema</param>
        /// <returns>The SQL Schema as a string</returns>
        public static string CreateSQLTablePrimaryKeyContrainst(DataTable sqlschema, string tablename)
        {
            if (HasPrimaryKey(sqlschema) == true)
            {
                bool firstfield = true;
                // Persons ADD PRIMARY KEY (P_Id)
                DBDataType dt = new DBDataType();
                StringBuilder retval = new StringBuilder("ALTER TABLE [" + tablename + "]\n" +
                "ADD PRIMARY KEY \n", 10);                
                retval.Append("(");
                // Loop through each column
                for (int i = 0; i < sqlschema.Rows.Count; i++)
                {
                    if (sqlschema.Rows[i]["IsKey"].Equals("true"))
                    {                        
                        //ProviderType
                        if (firstfield == false)
                        {
                            retval.Append(",");
                        }
                        else
                        {
                            firstfield = false;
                        }
                        // Column name
                        retval.Append("[" + sqlschema.Rows[i]["ColumnName"] + "]");                        
                    }
                }
                retval.Append("\n);");                

                return retval.ToString();
            }
            else
            {
                return "";
            }
        }

        

        /// <summary>
        /// Creates a table schema from a MSSQL query
        /// </summary>
        /// <param name="sqlschema">The data table with the SQL Schema</param>
        /// <returns>The SQL Lite Schema</returns>
        public static string CreateSQLInsertHeader(DataTable sqlschema, string tablename)
        {
            DBDataType dt = new DBDataType();
            StringBuilder retval = new StringBuilder("INSERT INTO ", 10);
            retval.Append(tablename);
            retval.Append("(");
            // Loop through each column
            for (int i = 0; i < sqlschema.Rows.Count; i++)
            {
                //ProviderType
                if (i > 0)
                {
                    retval.Append(",");
                }
                // Column name
                retval.Append("[" + sqlschema.Rows[i]["ColumnName"] + "]");
            }
            retval.Append(")");
            return retval.ToString();
        }
    }
}
