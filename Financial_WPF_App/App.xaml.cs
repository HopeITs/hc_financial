using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Financial_WPF_App
{ 
    public partial class App : Application
    {
        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF1cXmhKYVFwWmFZfVtgc19EZVZTRGYuP1ZhSXxWdkdhXX9ZcnFXQmFbWUE=");
        }
    }
}
