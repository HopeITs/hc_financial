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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzc4ODI1M0AzMjM4MmUzMDJlMzBXdEdJTm1LQlBkL3BWNkM2a29WRFdTWkxQVEw0QnFFd3lYTnFQek9GaTlJPQ==");
        }
    }
}
