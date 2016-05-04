using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSQLScriptGenerator
{
    class DBDataType
    {
        private Dictionary<string, bool> escapabletypes = new Dictionary<string, bool>()
            {
                {"varchar" ,true},
                {"smallint" ,false},
                {"real" ,false},
                {"text" ,true},
                {"char" ,true},
                {"nchar" ,true},
                {"decimal" ,false},
                {"tinyint" ,false},
                {"timestamp" ,true},
                {"bigint" ,false},
                {"nvarchar" ,true},
                {"int" ,false},
                {"smalldatetime" ,true},
                {"datetime" ,true},
                {"xml" ,true},
                {"varbinary" ,false},
                {"uniqueidentifier" ,true},
                {"bit" ,false}
            };

        private Dictionary<string, bool> sizeabletypes = new Dictionary<string, bool>()
            {
                {"varchar" ,true},
                {"smallint" ,false},
                {"real" ,false},
                {"text" ,false},
                {"char" ,true},
                {"nchar" ,true},
                {"decimal" ,false},
                {"tinyint" ,false},
                {"timestamp" ,false},
                {"bigint" ,false},
                {"nvarchar" ,true},
                {"int" ,false},
                {"smalldatetime" ,false},
                {"datetime" ,false},
                {"xml" ,false},
                {"varbinary" ,true},
                {"uniqueidentifier" ,false},
                {"bit" ,false}
            };

        public bool Escapable(string name)
        {
            return escapabletypes[name];
        }

        public bool Sizeable(string name)
        {
            return sizeabletypes[name];
        }

    }
}
