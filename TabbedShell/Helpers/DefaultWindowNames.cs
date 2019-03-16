using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabbedShell.Helpers
{
    public static class DefaultWindowNames
    {
        public static string GetName(string executableName)
        {
            switch (executableName.ToLower())
            {
                case "cmd.exe":
                    return "Command Prompt";
                case "powershell.exe":
                    return "Windows PowerShell";
                case "bash.exe":
                    return "Bash";
                default:
                    return "Terminal";
            }
        }
    }
}
