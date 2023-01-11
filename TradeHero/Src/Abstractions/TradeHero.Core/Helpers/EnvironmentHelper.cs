using System.Globalization;
using System.Runtime.InteropServices;

namespace TradeHero.Core.Helpers;

public static class EnvironmentHelper
{
    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
    
    public static void SetCulture()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }

    public static void SetFullPermissionsToFileLinux(string filePath)
    {
        chmod(filePath, 0x1 | 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x100);
    }
}