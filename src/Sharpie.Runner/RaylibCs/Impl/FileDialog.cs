using System.Runtime.InteropServices;

namespace Sharpie.Runner.RaylibCs.Impl;

internal static class FileDialog
{
    public static string? OpenFileDialog(string filter = "Sharpie ROM (*.shr)|*.shr")
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OpenFileDialogWindows(filter);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OpenFileDialogLinux();
        }
        return null;
    }

    private static string? OpenFileDialogWindows(string filter)
    {
        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.lpstrFilter = "Sharpie ROM Files\0*.shr\0All Files\0*.*\0\0";
            ofn.lpstrFile = new string('\0', 256);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string('\0', 64);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "Select Sharpie Cartridge";
            ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800; // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST

            if (GetOpenFileName(ref ofn))
            {
                return ofn.lpstrFile;
            }
        }
        catch { }
        return null;
    }

    private static string? OpenFileDialogLinux()
    {
        try
        {
            // Try using zenity if available
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Select Sharpie Cartridge\" --file-filter=\"Sharpie ROM (*.shr) | *.shr\" --file-filter=\"All files | *\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            
            if (process.ExitCode == 0 && !string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        catch { }
        
        return null;
    }

    // Windows P/Invoke
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public nint lCustData;
        public nint lpfnHook;
        public string lpTemplateName;
        public nint pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName(ref OPENFILENAME ofn);
}
