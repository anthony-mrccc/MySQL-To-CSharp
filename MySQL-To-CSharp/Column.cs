using System;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

namespace MySQL_To_CSharp
{
    public class Column
    {
        public Column(MySqlDataReader reader)
        {
            Name = reader.GetString(1);
            ColumnType = reader.GetString(2);
            IsPrimary = reader.GetString(3) == "PRI";
        }

        public string Name { get; set; }
        public Type Type { get; set; }
        public string ColumnType { get; set; }
        public bool IsPrimary { get; private set; }

        public override string ToString()
        {
            string code = string.Empty;
            if (IsPrimary)
                code += $"[DbPrimary]\r\n";
            code += $"[DbName(\"{Name}\")]\r\npublic {Type.Name}? {Name.FirstCharUpper()} {{ get; set; }}";
            return code;
        }
    }
}