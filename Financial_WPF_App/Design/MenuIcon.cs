using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial_WPF_App
{
    public class MenuIcon
    {
        public string IconPath { get; set; }
        public string HoverIconPath { get; set; }

        // Public parameterless constructor is necessary
        public MenuIcon(){}

        public MenuIcon(string iconPath, string hoverIconPath)
        {
            IconPath = iconPath;
            HoverIconPath = hoverIconPath;
        }
    }
}