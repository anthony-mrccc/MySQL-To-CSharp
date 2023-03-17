using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQL_To_CSharp
{
    public static class DesignerGenerator
    {
        public static string GenerateBtn(string name, float fontsize, string text ,Point location, Size size, int tabindex, bool clickev)
        {
            string source = $@"
            this.btn{name}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {fontsize}F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn{name}.Location = new System.Drawing.Point({location.X}, {location.Y});
            this.btn{name}.Name = ""btn{name}"";
            this.btn{name}.Size = new System.Drawing.Size({size.Width}, {size.Height});
            this.btn{name}.TabIndex = {tabindex};
            this.btn{name}.Text = ""{text}"";
            this.btn{name}.UseVisualStyleBackColor = true;
            {(clickev ? GenerateClickEvent(name) : "")}";
            return source;
        }

        private static string GenerateClickEvent(string name)
        {
            return $@"this.btn{name}.Click += new System.EventHandler(this.btn{name}_Click);";
        }

        public static string GenerateLabel(string name, float fontsize, string text, Point location, int tabindex)
        {
            string source = $@"
            this.lbl{name}.AutoSize = true;
            this.lbl{name}.Font = new System.Drawing.Font(""Microsoft Sans Serif"", {fontsize}, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl{name}.Location = new System.Drawing.Point({location.X}, {location.Y});
            this.lbl{name}.Name = ""lbl{name}"";
            this.lbl{name}.Size = new System.Drawing.Size(44, 13);
            this.lbl{name}.TabIndex = {tabindex};
            this.lbl{name}.Text = ""{text}:"";";
            return source;
        }

        private static string GenerateInitializeComponentNewStatements(Dictionary<string, List<Element>> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                foreach (var el in item.Value)
                {
                    sb.Append(el.GenerateComponentNewStatement());
                }
            }
            sb.Append("\r\nthis.SuspendLayout();");
            return sb.ToString();
        }

        private static string GenerateElements(Dictionary<string, List<Element>> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                foreach (var el in item.Value)
                {
                    sb.Append(el.GenerateProps());
                }
            }
            return sb.ToString();
        }

        private static string GenerateControlsAdd(Dictionary<string, List<Element>> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                foreach (var el in item.Value)
                {
                    sb.AppendLine(el.GenerateControlsAdd());
                }
            }
            return sb.ToString();
        }

        private static string GenerateEndInit(Dictionary<string, List<Element>> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                foreach (var el in item.Value)
                {
                    sb.Append(el.GenerateEndInit());
                }
            }
            return sb.ToString();
        }

        public static string GenerateInitializeComponent(Dictionary<string, List<Element>> dict, string className)
        {
            string source = $@"
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {{
            {GenerateInitializeComponentNewStatements(dict)}
            {GenerateElements(dict)}
            // 
            // {className}
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 450);
            {GenerateControlsAdd(dict)}
            this.Name = ""{className}"";
            {GenerateEndInit(dict)}
            this.ResumeLayout(false);
            this.PerformLayout();

        }}";
            return source;
        }

        public static string GenerateFields(Dictionary<string, List<Element>> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                foreach (var el in item.Value)
                {
                    sb.AppendLine(el.GenerateFields());
                }
            }
            return sb.ToString();
        }
    }
}
