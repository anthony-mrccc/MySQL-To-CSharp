using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MySQL_To_CSharp
{
    public class Element
    {
        public ColName ColName { get; set; }
        public float FontSize { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
        public int TabIndex { get; set; }
        public string Text { get; set; }
        public bool ClickEvent { get; set; }
        public Type Type { get; set; }
        public Column Column { get; set; }

        public Element(ColName colName)
        {
            ColName = colName;
        }

        public string GenerateFields()
        {
            return $"System.Windows.Forms.{ColName.WinFormsClass} {ColName.FullName};";
        }

        public string GenerateProps()
        {
            switch (ColName.WinFormsClass)
            {
                case "Button":
                    return GeneratePropsButton();
                case "Label":
                    return GeneratePropsLabel();
                case "NumericUpDown":
                    return GeneratePropsNumericUpDown();
                case "TextBox":
                    return GeneratePropsTextBox();
                case "DateTimePicker":
                    return GeneratePropsDateTimePicker();
                default:
                    return "";
            }
        }

        public string GenerateComponentNewStatement()
        {
            string source = $@"
            this.{ColName.FullName} = new System.Windows.Forms.{ColName.WinFormsClass}();";
            if (ColName.WinFormsClass == "NumericUpDown")
                source += $"\r\n((System.ComponentModel.ISupportInitialize)(this.{ColName.FullName})).BeginInit();";
            return source;
        }

        public string GenerateControlsAdd()
        {
            return $"this.Controls.Add(this.{ColName.FullName});";
        }

        public string GenerateEndInit()
        {
            if (ColName.WinFormsClass == "NumericUpDown")
                return $"((System.ComponentModel.ISupportInitialize)(this.{ColName.FullName})).EndInit();";
            return "";
        }

        private string GeneratePropsButton()
        {
            string source = $@"
            // 
            // {ColName.FullName}
            //
            this.{ColName.FullName}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {FontSize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.{ColName.FullName}.Location = new System.Drawing.Point({Location.X},{Location.Y});
            this.{ColName.FullName}.Name = ""{ColName.FullName}"";
            this.{ColName.FullName}.Size = new System.Drawing.Size({Size.Width}, {Size.Height});
            this.{ColName.FullName}.TabIndex = {TabIndex};
            this.{ColName.FullName}.Text = ""{Text}"";
            this.{ColName.FullName}.UseVisualStyleBackColor = true;
            {(ClickEvent ? $@"this.{ColName.FullName}.Click += new System.EventHandler(this.{ColName.FullName}_Click);" : "")}";
            return source;
        }
        private string GeneratePropsLabel()
        {
            string source = $@"
            // 
            // {ColName.FullName}
            // 
            this.{ColName.FullName}.AutoSize = true;
            this.{ColName.FullName}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {FontSize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.{ColName.FullName}.Location = new System.Drawing.Point({Location.X}, {Location.Y});
            this.{ColName.FullName}.Name = ""{ColName.FullName}"";
            this.{ColName.FullName}.Size = new System.Drawing.Size({Size.Width}, {Size.Height});
            this.{ColName.FullName}.TabIndex = {TabIndex};
            this.{ColName.FullName}.Text = ""{Text}:"";";
            return source;
        }
        private string GeneratePropsNumericUpDown()
        {
            string source = $@"
            // 
            // {ColName.FullName}
            // 
            this.{ColName.FullName}.Location = new System.Drawing.Point({Location.X}, {Location.Y});
            this.{ColName.FullName}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {FontSize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.{ColName.FullName}.Name = ""{ColName.FullName}"";
            this.{ColName.FullName}.Size = new System.Drawing.Size({Size.Width}, {Size.Height});
            this.{ColName.FullName}.TabIndex = {TabIndex};";
            return source;
        }
        private string GeneratePropsTextBox()
        {
            string source = $@"
            // 
            // {ColName.FullName}
            // 
            this.{ColName.FullName}.Location = new System.Drawing.Point({Location.X}, {Location.Y});
            this.{ColName.FullName}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {FontSize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.{ColName.FullName}.Name = ""{ColName.FullName}"";
            this.{ColName.FullName}.Size = new System.Drawing.Size({Size.Width}, {Size.Height});
            this.{ColName.FullName}.TabIndex = {TabIndex};";
            return source;
        }
        private string GeneratePropsDateTimePicker()
        {
            string source = $@"
            // 
            // {ColName.FullName}
            // 
            this.{ColName.FullName}.Location = new System.Drawing.Point({Location.X}, {Location.Y});
            this.{ColName.FullName}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {FontSize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.{ColName.FullName}.Name = ""{ColName.FullName}"";
            this.{ColName.FullName}.Size = new System.Drawing.Size({Size.Width}, {Size.Height});
            this.{ColName.FullName}.TabIndex = {TabIndex};
";
            return source;
        }
    }
}
