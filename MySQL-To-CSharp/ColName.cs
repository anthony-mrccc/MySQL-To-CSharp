using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQL_To_CSharp
{
    public class ColName
    {
        public string NameNoCharUpper { get; set; }
        public string Name {  get; set; }
        public string Suffix { get; set; }
        public string WinFormsClass { get; set; }
        public string FullName { get => Suffix + Name; }

        public ColName(Column col)
        {
            NameNoCharUpper = col.Name;
            Name = col.Name.FirstCharUpper();
            Suffix = ResolveTypeSuffix(col);
            WinFormsClass = ResolveTypeParseWinFormsType(col);
        }

        public ColName(string nameNoCharUpper, string name, string suffix, string winFormsClass)
        {
            NameNoCharUpper = nameNoCharUpper;
            Name = name;
            Suffix = suffix;
            WinFormsClass = winFormsClass;
        }

        public static string ResolveTypeSuffix(Column col)
        {
            if (col.Type == typeof(int) || col.Type == typeof(float) || col.Type == typeof(double))
                return "nud";
            if (col.Type == typeof(string))
                return "tbx";
            if (col.Type == typeof(DateTime))
                return "dtp";
            return "tbx";
        }

        public static string ResolveTypeParseWinFormsType(Column col)
        {
            if (col.Type == typeof(int) || col.Type == typeof(float) || col.Type == typeof(double))
                return "NumericUpDown";
            if (col.Type == typeof(string))
                return "TextBox";
            if (col.Type == typeof(DateTime))
                return "DateTimePicker";
            return "TextBox";
        }
    }
}
