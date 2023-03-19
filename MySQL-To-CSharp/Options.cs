using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQL_To_CSharp
{
    public static class Options
    {
        public const string LABEL_SUFFIX = "lbl";
        public const string LABEL_WINFORMS_CLASS = "Label";
        public const string BUTTON_SUFFIX = "btn";
        public const string BUTTON_WINFORMS_CLASS = "Button";
        public static readonly Point startPoint = new Point(25, 40);
        public const int DISTANCE_LABEL_ELEMENT = 125;
        public const int ELEMENT_SIZE = 275;
        public const int SPACE_ELEMENTS = 50;
        public const string DTO_CONTAINERS_NMSP_PATH = ".DTO.Containers";
        public const string DTOFORMS_NMSP_PATH = ".DTOContextForms";
        public const int ELEMENT_SIZE_SEARCH = 200;
    }
}
