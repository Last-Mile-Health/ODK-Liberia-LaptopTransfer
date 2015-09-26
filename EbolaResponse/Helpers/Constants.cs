using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LastMileHealth.Helpers
{
    public static class Constants
    {
        public const string DebugOnly = "[Debug only]: ";
        public static readonly string ODKPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODKBluetoothTransfer");
        public static readonly string FormsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODKBluetoothTransfer", "Forms");
        public const string ServiceClassId = "fa87c0d0-afac-11de-8a39-0800200c9a66";
        public const string RolesSection = "ROLES";
    }
}
