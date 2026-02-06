using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial_WPF_App.Helper
{
    internal class TillNotifier
    {
        public static event Action RefreshTill;

        public static void RaiseRefresh()
        {
            RefreshTill?.Invoke();
        }
    }
}
