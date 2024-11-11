using System.Collections.Generic;

namespace Financial_WPF_App.Helper
{
    public class TreeViewItemViewModel
    {
        public string Name { get; set; }

        public List<TreeViewItemViewModel> Children { get; set; }

        public TreeViewItemViewModel(string name)
        {
            Name = name;
            Children = new List<TreeViewItemViewModel>();
        }

        public class city
        {
            public string CityName { get; set; }
            public List<project> Projects { get; set; }
        }

        public class project
        {
            public string ProjectName { get; set; }
            public List<subproject> Subprojects { get; set; }
        }

        public class subproject
        {
            public string SubprojectName { get; set; }
            public List<account> Accounts { get; set; }
        }

        public class account
        {
            public string AccountName { get; set; }
        }
    }

}
