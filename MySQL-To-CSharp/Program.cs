﻿/*
 * WARNING : THIS NEED A LOT OF REFACTORING !!!!!!!!!!!!! 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Xml.Linq;
using Fclp;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Linq;

namespace MySQL_To_CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parser = new FluentCommandLineParser<ApplicationArguments>();
            parser.Setup(arg => arg.IP).As('i', "ip").SetDefault("127.0.0.1")
                .WithDescription("(optional) IP address of the MySQL server, will use 127.0.0.1 if not specified");
            parser.Setup(arg => arg.Port).As('n', "port").SetDefault(3306)
                .WithDescription("(optional) Port number of the MySQL server, will use 3306 if not specified");
            parser.Setup(arg => arg.User).As('u', "user").SetDefault("root")
                .WithDescription("(optional) Username, will use root if not specified");
            parser.Setup(arg => arg.Password).As('p', "password").SetDefault(string.Empty)
                .WithDescription("(optional) Password, will use empty password if not specified");
            parser.Setup(arg => arg.Database).As('d', "database").Required().WithDescription("Database name");
            parser.Setup(arg => arg.Table).As('t', "table").SetDefault(string.Empty)
                .WithDescription("(optional) Table name, will generate entire database if not specified");
            parser.Setup(arg => arg.Namespace).As("ns").SetDefault(string.Empty)
                .WithDescription("namespace");
            parser.Setup(arg => arg.GenerateConstructorAndOutput).As('g', "generatequery")
                .SetDefault(false)
                .WithDescription(
                    "(optional) Generate a reading constructor - Activate with -c true");
            parser.Setup(arg => arg.Constructor).As('c', "generateconstructor")
                .SetDefault(false)
                .WithDescription(
                    "(optional) Generate a reading constructor and SQL statement output - Activate with -g true");
            parser.Setup(arg => arg.GenerateMarkupPages).As('m', "generatemarkuppages")
                .SetDefault(false)
                .WithDescription(
                    "(optional) Generate markup pages for database and tables which can be used in wikis - Activate with -m true");
            parser.Setup(arg => arg.MarkupDatabaseNameReplacement).As('r', "markupdatabasenamereplacement")
                .SetDefault("")
                .WithDescription("(optional) Will use this instead of database name for wiki breadcrump generation");
            parser.Setup(arg => arg.Path).As('o')
                .WithDescription("(optional) Output path");
            parser.SetupHelp("?", "help").Callback(text => Console.WriteLine(text));

            var result = parser.Parse(args);
            if (!result.HasErrors)
            {
                var conf = parser.Object;
                if (conf.Database is null)
                {
                    Console.WriteLine("You didn't specify a database");
                    return;
                }

                var confString =
                    $"Server={conf.IP};Port={conf.Port};Uid={conf.User};Pwd={conf.Password};Database={conf.Database}";
                Console.WriteLine(confString);

                var database = new Dictionary<string, List<Column>>();

                using (var con = new MySqlConnection(confString))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText =
                            $"SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, COLUMN_KEY FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{conf.Database}'";
                        if (!conf.Table.Equals(string.Empty))
                            cmd.CommandText += $" AND TABLE_NAME = '{conf.Table}'";

                        var reader = cmd.ExecuteReader();
                        if (!reader.HasRows)
                            return;

                        while (reader.Read())
                            if (database.ContainsKey(reader.GetString(0)))
                                database[reader.GetString(0)].Add(new Column(reader));
                            else
                                database.Add(reader.GetString(0), new List<Column> { new Column(reader) });

                        reader.Close();
                    }

                    foreach (var table in database)
                        using (var cmd = con.CreateCommand())
                        {
                            // TODO: Is there a way to do this without this senseless statement?
                            cmd.CommandText = $"SELECT * FROM `{table.Key}` LIMIT 0";
                            var reader = cmd.ExecuteReader();
                            var schema = reader.GetSchemaTable();
                            foreach (var column in table.Value)
                                column.Type = schema.Select($"ColumnName = '{column.Name}'")[0]["DataType"] as Type;

                            reader.Close();
                        }

                    con.Close();
                }

                string path = conf.Database;
                if (conf.Path != "" && conf != null)
                    path = conf.Path;

                Stopwatch sw = Stopwatch.StartNew();
                DbToClasses(conf.Database, database, conf.GenerateConstructorAndOutput, conf.Constructor, conf.Namespace, path + @"\DTO\Containers\" + conf.Database);
                // Search
                DbToForm(path + @"\DTOContextForms\" + conf.Database + @"\search\", conf.Namespace, "SearchForm", "Search", database, "return", "Retour", "search", "Rechercher", "", "", true);
                DbToDesigner(path + @"\DTOContextForms\" + conf.Database + @"\search\", conf.Namespace, "SearchForm", database, "return", "Retour", "search", "Rechercher", false, true);

                // Update
                DbToForm(path + @"\DTOContextForms\" + conf.Database + @"\update\", conf.Namespace, "UpdateForm", "Update", database, "return", "Retour", "update", "Appliquer", "IDTOContainer container", "container", false);
                DbToDesigner(path + @"\DTOContextForms\" + conf.Database + @"\update\", conf.Namespace, "UpdateForm", database, "return", "Retour", "update", "Appliquer", false, false);

                // Insert
                DbToForm(path + @"\DTOContextForms\" + conf.Database + @"\insert\", conf.Namespace, "InsertForm", "Insert", database, "return", "Retour", "insert", "Insérer", "", "", false);
                DbToDesigner(path + @"\DTOContextForms\" + conf.Database + @"\insert\", conf.Namespace, "InsertForm", database, "return", "Retour", "insert", "Insérer", true, false);

                // WinFormsExt
                GenerateDTOWinformsExt(path + @"\DTO\Containers\Ext", conf.Namespace, database);
                if (conf.GenerateMarkupPages)
                    DbToMarkupPage(
                        string.IsNullOrEmpty(conf.MarkupDatabaseNameReplacement)
                            ? conf.Database
                            : conf.MarkupDatabaseNameReplacement, database);
                sw.Stop();
                Console.WriteLine("Successfully generated C# classes in " + sw.ElapsedMilliseconds + "ms!");
            }

            Console.ReadLine();
        }

        private static void DbToForm(string path, string nmspace, string filename, string ctx, Dictionary<string, List<Column>> db, string btn1Name, string btn1Text, string btn2Name, string btn2Text, string args, string baseargs, bool isSearch)
        {
            string filePartial = "Frm";
            foreach (var item in db)
            {
                string className = filePartial + item.Key.FirstCharUpper() + filename;
                TableToForm(path, $"{className}.cs", className, item.Key.FirstCharUpper(), nmspace, ctx,item.Value, btn1Name, btn1Text, btn2Name, btn2Text, args, baseargs, isSearch);
            }
        }

        private static void TableToForm(string path, string filename, string className, string tablename, string nmspace, string ctx, List<Column> cols, string btn1Name, string btn1Text, string btn2Name, string btn2Text, string args, string baseargs, bool isSearch)
        {
            Dictionary<string, List<Element>> colNamesDict = PopulateDict(cols, (isSearch ? Options.ELEMENT_SIZE_SEARCH : Options.ELEMENT_SIZE),btn1Name, btn1Text, btn2Name, btn2Text, isSearch);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string source = $@"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using {nmspace}{Options.DTO_CONTAINERS_NMSP_PATH};

namespace {nmspace}{Options.DTOFORMS_NMSP_PATH}
{{
    public partial class {className}: Frm{ctx}
    {{
        public {className}({args}) {(baseargs.Length > 0 ? $":base({baseargs})" : "")}
        {{
            InitializeComponent();";
            if (ctx == "Update")
                source += "\r\nSetValues();";
            source += $@"
        }}

        private void btn{btn1Name.FirstCharUpper()}_Click(object sender, EventArgs e)
        {{
            {btn1Name.FirstCharUpper()}();
        }}

        private void btn{btn2Name.FirstCharUpper()}_Click(object sender, EventArgs e)
        {{
            {GenerateDTOConstruction(tablename + "DTOContainer", colNamesDict, btn2Name == "insert")}";
            if (ctx == "Search")
            {
                source += $@"
                string[] values = Controls.Cast<Control>()
                .Where(c => c is CheckBox chb)
                .Where(chb => ((CheckBox)chb).Checked)
                .Select(chb => chb.Tag.ToString())
                .ToArray();
            Search(container, values);";
            }
            else
            {
                source += $@"{btn2Name.FirstCharUpper()}(container);";
            }
            source += $@"}}";
            if (ctx == "Update")
            {
                source += $@"
        private void SetValues()
        {{
            Dictionary<string, object> reflectedValues = _container.ReflectProps();
            {GenerateDTOContainerValuesBinder(colNamesDict)}
        }}";
            }
source += $@"
    }}
}}
";
            var sw = new StreamWriter($"{path}\\{filename}", false);
            sw.Write(source);
            sw.Close();
        }

        private static string GenerateDTOContainerValuesBinder(Dictionary<string, List<Element>> colNamesDict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var els in colNamesDict.Values)
            {
                for (int i = 0; i < els.Count; i++)
                {
                    ColName colName = els[i].ColName;
                    if (colName.WinFormsClass != "Label" && colName.WinFormsClass != "Button")
                    {
                        if (colName.WinFormsClass == "TextBox")
                        {
                            sb.AppendLine($"{colName.FullName}.Text = ({els[i].Type.Name})reflectedValues[\"{colName.NameNoCharUpper}\"];");
                        }
                        else if (colName.WinFormsClass == "NumericUpDown")
                        {
                            sb.AppendLine($"{colName.FullName}.Value = ({els[i].Type.Name})reflectedValues[\"{colName.NameNoCharUpper}\"];");
                        }
                        else
                        {
                            sb.AppendLine($"{colName.FullName}.Value = ({els[i].Type.FullName})reflectedValues[\"{colName.NameNoCharUpper}\"];");
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private static string GenerateDTOConstruction(string dtoName, Dictionary<string, List<Element>> colNamesDict, bool primaryIsNulls)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"IDTOContainer container = new {dtoName}(");
            List<string> parameters = new List<string>();
            foreach (var els in colNamesDict.Values)
            {
                for (int i = 0; i < els.Count; i++)
                {
                    ColName colName = els[i].ColName;
                    if (colName.WinFormsClass != "Label" && colName.WinFormsClass != "Button" && colName.WinFormsClass != "CheckBox")
                    {
                        if (colName.WinFormsClass == "TextBox")
                        {
                            if (primaryIsNulls && els[i].Column.IsPrimary)
                                parameters.Add($"null");
                            else
                                parameters.Add($"{colName.FullName}.Text");
                        }
                        else if (colName.WinFormsClass == "NumericUpDown")
                        {
                            if (primaryIsNulls && els[i].Column.IsPrimary)
                                parameters.Add($"null");
                            else
                                parameters.Add($"({els[i].Type.Name}?){colName.FullName}.Value");
                        }
                        else
                        {
                            if (primaryIsNulls && els[i].Column.IsPrimary)
                                parameters.Add($"null");
                            else
                                parameters.Add($"{colName.FullName}.Value");
                        }
                    }
                }
            }
            sb.Append(string.Join(",", parameters));
            sb.AppendLine(");");
            return sb.ToString();
        }

        private static void DbToDesigner(string path, string nmspace, string filename, Dictionary<string, List<Column>> db, string btn1Name, string btn1Text, string btn2Name, string btn2Text, bool excludePrimarys, bool isSearch)
        {
            if (excludePrimarys)
            {
                Dictionary<string, List<Column>> colNamesDictNoPri = new Dictionary<string, List<Column>>();
                foreach (var item in db)
                {
                    List<Column> colsNoPri = item.Value.Where(i => i.IsPrimary == false).ToList();
                    colNamesDictNoPri.Add(item.Key, colsNoPri);
                }
                db = colNamesDictNoPri;
            }

            string filePartial = "Frm";
            foreach (var item in db)
            {
                string className = filePartial + item.Key.FirstCharUpper() + filename;
                TableToDesigner(path, $"{className}.Designer.cs", className, nmspace, item.Key, item.Value, btn1Name, btn1Text, btn2Name, btn2Text, isSearch);
            }
        }

        private static void TableToDesigner(string path, string filename, string className, string nmspace, string nm, List<Column> cols, string btn1Name, string btn1Text, string btn2Name, string btn2Text, bool isSearch)
        {
            nm.FirstCharUpper();

            Dictionary<string, List<Element>> colNamesDict = PopulateDict(cols, (isSearch ? Options.ELEMENT_SIZE_SEARCH : Options.ELEMENT_SIZE),btn1Name, btn1Text, btn2Name, btn2Text, isSearch);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var sb = new StringBuilder();

            string source = GenerateTableDesignerSourceCode(nmspace, className, colNamesDict);

            var sw = new StreamWriter($"{path}\\{filename}", false);
            sw.Write(source);
            sw.Close();
            sb.Clear();
        }

        private static Dictionary<string, List<Element>> PopulateDict(List<Column> cols, int elementsize, string btn1Name, string btn1Text, string btn2Name, string btn2Text, bool isSearch)
        {
            Dictionary<string, List<Element>> colNamesDict = new Dictionary<string, List<Element>>();
            // populate dict
            int i = 0;
            foreach (var col in cols)
            {
                List<Element> els = new List<Element>()
                {
                    new Element(new ColName(col.Name, col.Name.FirstCharUpper(),Options.LABEL_SUFFIX, Options.LABEL_WINFORMS_CLASS)) // Label
                    {
                        FontSize = 14F,
                        Location = new Point(Options.startPoint.X, Options.startPoint.Y + Options.SPACE_ELEMENTS * i),
                        Size = new Size(30,30),
                        TabIndex = 3*i,
                        Text = col.Name.FirstCharUpper(),
                    },
                    new Element(new ColName(col)) // Field
                    {
                        FontSize = 14F,
                        Location = new Point(Options.startPoint.X + Options.DISTANCE_LABEL_ELEMENT,Options.startPoint.Y + Options.SPACE_ELEMENTS * i),
                        Size = new Size(elementsize,20),
                        TabIndex = 3*i + 1,
                        Text = col.Name.FirstCharUpper(),
                        Type = col.Type,
                        Column = col
                    }
                };
                if (isSearch)
                {
                    Element el = new Element(new ColName($"{col.Name}", $"Include{col.Name.FirstCharUpper()}", "chb", "CheckBox")) // CheckBox
                    {
                        FontSize = 14F,
                        Location = new Point(Options.startPoint.X + elementsize + Options.DISTANCE_LABEL_ELEMENT + 10, Options.startPoint.Y + Options.SPACE_ELEMENTS * i),
                        Size = new Size(elementsize, 20),
                        TabIndex = 3*i + 2,
                        Text = "Include",
                        Type = col.Type,
                        Column = col
                    };
                    els.Add(el);
                }
                colNamesDict.Add(col.Name, els);
                i++;
            }
            colNamesDict.Add(btn1Name, new List<Element>() {
                new Element(new ColName(btn1Name, btn1Name.FirstCharUpper(), Options.BUTTON_SUFFIX, Options.BUTTON_WINFORMS_CLASS))
                {
                    FontSize = 18F,
                    Location = new Point(231,358),
                    Size = new Size(209,80),
                    TabIndex = i+1,
                    Text = btn1Text,
                    ClickEvent = true
                }
            });
            colNamesDict.Add(btn2Name, new List<Element>() {
                new Element(new ColName(btn2Name, btn2Name.FirstCharUpper(), Options.BUTTON_SUFFIX, Options.BUTTON_WINFORMS_CLASS))
                {
                    FontSize = 18F,
                    Location = new Point(12,358),
                    Size = new Size(209,80),
                    TabIndex = i+2,
                    Text = btn2Text,
                    ClickEvent = true
                }
            });
            return colNamesDict;
        }

        private static void GenerateDTOWinformsExt(string path, string nmspace, Dictionary<string, List<Column>> db)
        {
            string source = $@"
using {nmspace}.DTOContextForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace {nmspace}.DTO.Containers.Ext
{{
    public static class DTOWinformsExt
    {{
        public static void ShowSearchForm(this IDTOContainer dtoContainer) 
        {{
            {GenerateWinFormsExtConditions("Search", "",db, false, false)}
        }}
        public static FrmUpdate? ShowUpdateForm(this IDTOContainer dtoContainer) 
        {{
            FrmUpdate? frm = null;
            {GenerateWinFormsExtConditions("Update", "dtoContainer", db, true, true)}
            return frm;
        }}
        public static void ShowInsertForm(this IDTOContainer dtoContainer) 
        {{
            {GenerateWinFormsExtConditions("Insert", "",db, false, false)}
        }}

    }}
}}

";
            var sw = new StreamWriter($"{path}/DTOWinformsExt.cs", false);
            sw.Write(source);
            sw.Close();
        }

        private static string GenerateWinFormsExtConditions(string ctx, string parameters, Dictionary<string, List<Column>> db, bool showDialog, bool isUpdate)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var col in db)
            {
                sb.Append(GenerateWinFormsExtCondition(ctx, col.Key.FirstCharUpper(), parameters, showDialog, isUpdate));
            }
            return sb.ToString();
        }

        private static string GenerateWinFormsExtCondition(string ctx, string tableName,string parameters, bool showDialog, bool isUpdate)
        {
            string source = $@"
            if (dtoContainer is {tableName}DTOContainer)
            {{
                {(isUpdate ? "" : $"Frm{tableName}{ctx}Form")} frm = new Frm{tableName}{ctx}Form({parameters});
                {(showDialog ? "frm.DialogResult = frm.ShowDialog();" : "frm.Show();")}
            }}";
            return source;
        }

        private static string GenerateTableDesignerSourceCode(string nmspace, string className, Dictionary<string, List<Element>> colNamesDict)
        {


            string source = $@"
namespace {nmspace}{Options.DTOFORMS_NMSP_PATH}
{{
    partial class {className}
    {{
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name=""disposing"">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {{
            if (disposing && (components != null))
            {{
                components.Dispose();
            }}
            base.Dispose(disposing);
        }}

        #region Windows Form Designer generated code
        {DesignerGenerator.GenerateInitializeComponent(colNamesDict, className)}
        #endregion
        {DesignerGenerator.GenerateFields(colNamesDict)}
    }}
}}
";
            return source;
        }

        private static void DbToClasses(string dbName, Dictionary<string, List<Column>> db,
            bool generateConstructorAndOutput, bool generateCtor, string nmspace, string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var sb = new StringBuilder();
            foreach (var table in db)
            {
                // usings
                sb.AppendLine("using System;\r\nusing System.Data;\r\n");

                sb.AppendLine($"namespace {nmspace}{Options.DTO_CONTAINERS_NMSP_PATH}");
                sb.AppendLine("{");
                sb.AppendLine($"[DbTableName(\"{table.Key}\")]");
                sb.AppendLine($"public class {table.Key.FirstCharUpper()}DTOContainer : DTOContainer");
                sb.AppendLine("{");

                // properties
                foreach (var column in table.Value)
                    sb.AppendLine(column.ToString());

                if (generateCtor)
                {
                    // constructor
                    sb.AppendLine($"{Environment.NewLine}public {table.Key.FirstCharUpper()}DTOContainer(IDataReader reader) : base(reader) {{ }} {Environment.NewLine}");
                    // second ctor
                    sb.Append($"{Environment.NewLine}public {table.Key.FirstCharUpper()}DTOContainer(");
                    string[] parameters = new string[table.Value.Count];
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        parameters[j] = $"{table.Value[j].Type.Name}? {table.Value[j].Name}";
                    }
                    sb.Append(string.Join(",", parameters));
                    sb.AppendLine(")\r\n{");
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        sb.AppendLine($"this.{table.Value[j].Name.FirstCharUpper()} = {table.Value[j].Name};");
                    }
                    sb.AppendLine("}");
                }
                if (generateConstructorAndOutput)
                {
                    // update query
                    sb.AppendLine("public string UpdateQuery()");
                    sb.AppendLine("{");
                    sb.Append($"return $\"UPDATE `{table.Key}` SET");
                    foreach (var column in table.Value)
                        sb.Append($" {column.Name} = {{{column.Name.FirstCharUpper()}}},");
                    sb.Remove(sb.ToString().LastIndexOf(','), 1);
                    sb.AppendLine($" WHERE {table.Value[0].Name} = {{{table.Value[0].Name.FirstCharUpper()}}};\";");
                    sb.AppendLine($"}}{Environment.NewLine}");

                    // insert query
                    sb.AppendLine("public string InsertQuery()");
                    sb.AppendLine("{");
                    sb.Append($"return $\"INSERT INTO `{table.Key}` VALUES (");
                    foreach (var column in table.Value)
                        sb.Append($" {{{column.Name.FirstCharUpper()}}},");
                    sb.Remove(sb.ToString().LastIndexOf(','), 1);
                    sb.AppendLine($");\";{Environment.NewLine}}}{Environment.NewLine}");

                    // delete query
                    sb.AppendLine("public string DeleteQuery()");
                    sb.AppendLine("{");
                    sb.AppendLine(
                        $"return $\"DELETE FROM `{table.Key}` WHERE {table.Value[0].Name} = {{{table.Value[0].Name.FirstCharUpper()}}};\";");
                    sb.AppendLine("}");
                }

                // DTOContainer impl
                sb.AppendLine("public override void AddFields(IDataReader reader)");
                sb.AppendLine("{");
                int i = 0;
                foreach (var column in table.Value)
                {
                    // check which type and use correct get method instead of casting
                    if (column.Type != typeof(string))
                        sb.AppendLine($"{column.Name.FirstCharUpper()} = reader.Get{column.Type.Name}({i});");
                    else
                        sb.AppendLine($"{column.Name.FirstCharUpper()} = reader.GetString({i});");
                    i++;
                }
                sb.AppendLine("}");



                // class closing
                sb.AppendLine("}");
                // namespace closing
                sb.AppendLine("}");


                var sw = new StreamWriter($"{path}/{table.Key.FirstCharUpper()}DTOContainer.cs", false);
                sw.Write(sb.ToString());
                sw.Close();
                sb.Clear();
            }
        }

        private static void DbToMarkupPage(string dbName, Dictionary<string, List<Column>> db)
        {
            var wikiDir = "wiki";
            var wikiDbDir = $"{wikiDir}/{dbName}";
            var wikiTableDir = $"{wikiDbDir}/tables";

            if (!Directory.Exists(wikiDir))
                Directory.CreateDirectory(wikiDir);
            if (!Directory.Exists(wikiTableDir))
                Directory.CreateDirectory(wikiTableDir);

            var sb = new StringBuilder();

            sb.AppendLine($"* [[{dbName}|{dbName}]]");

            var sw = new StreamWriter($"{wikiDir}/index.txt", true);
            sw.Write(sb.ToString());
            sw.Close();
            sb.Clear();

            sb.AppendLine($"[[Database Structure|Database Structure]] > [[{dbName}|{dbName}]]");

            // generate index pages
            foreach (var table in db)
                sb.AppendLine($"* [[{table.Key.FirstCharUpper()}|{table.Key.ToLower()}]]");

            sw = new StreamWriter($"{wikiDbDir}/{dbName}.txt");
            sw.Write(sb.ToString());
            sw.Close();
            sb.Clear();

            foreach (var table in db)
            {
                sb.AppendLine(
                    $"[[Database Structure|Database Structure]] > [[{dbName}|{dbName}]] > [[{table.Key}|{table.Key}]]");
                sb.AppendLine("");
                sb.AppendLine("Column | Type | Description");
                sb.AppendLine("--- | --- | ---");

                foreach (var column in table.Value)
                    sb.AppendLine($"{column.Name.FirstCharUpper()} | {column.ColumnType} | ");
                sw = new StreamWriter($"{wikiTableDir}/{table.Key}.txt");
                sw.Write(sb.ToString());
                sw.Close();
                sb.Clear();
            }
        }
    }
}