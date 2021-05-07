using HarmonyLib;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Wpf;

using System;
using System.Reflection;
using System.Windows;
using System.Threading.Tasks;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Ptc.PersistentData;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShareCad
{
    static class WinConsole
    {
        static public void Initialize(bool alwaysCreateNewConsole = true)
        {
            bool consoleAttached = true;
            if (alwaysCreateNewConsole
                || (AttachConsole(ATTACH_PARRENT) == 0
                && Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
            {
                consoleAttached = AllocConsole() != 0;
            }

            if (consoleAttached)
            {
                InitializeOutStream();
                InitializeInStream();
            }
        }

        private static void InitializeOutStream()
        {
            var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
            if (fs != null)
            {
                var writer = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
        }

        private static void InitializeInStream()
        {
            var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
            if (fs != null)
            {
                Console.SetIn(new StreamReader(fs));
            }
        }

        private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
                                FileAccess dotNetFileAccess)
        {
            var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
            if (!file.IsInvalid)
            {
                var fs = new FileStream(file, dotNetFileAccess);
                return fs;
            }
            return null;
        }

        #region Win API Functions and Constants
        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateFileW(
              string lpFileName,
              UInt32 dwDesiredAccess,
              UInt32 dwShareMode,
              IntPtr lpSecurityAttributes,
              UInt32 dwCreationDisposition,
              UInt32 dwFlagsAndAttributes,
              IntPtr hTemplateFile
            );

        private const UInt32 GENERIC_WRITE = 0x40000000;
        private const UInt32 GENERIC_READ = 0x80000000;
        private const UInt32 FILE_SHARE_READ = 0x00000001;
        private const UInt32 FILE_SHARE_WRITE = 0x00000002;
        private const UInt32 OPEN_EXISTING = 0x00000003;
        private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        private const UInt32 ERROR_ACCESS_DENIED = 5;

        private const UInt32 ATTACH_PARRENT = 0xFFFFFFFF;

        #endregion
    }

    [HarmonyPatch]
    public class ShareCad
    {
        bool initialised;
        static EngineeringDocument engineeringDocument;
        static IWorksheetPersistentData testData;

        public void ShareCadInit()
        {
            if (!initialised)
            {
                var harmony = new Harmony("ShareCad");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                WinConsole.Initialize();
                initialised = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EngineeringDocument), "OpenFileAndUpdateCWDState")]
        public static void Prefix_EngineeringDocument(EngineeringDocument __instance, ref string fileName)
        {
            if (__instance != null)
            {
                engineeringDocument = __instance;
            }
        }

        static bool test;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action)
        {
            if (engineeringDocument != null)
            {
                if (!test)
                {
                    Console.WriteLine("test");
                    test = true;
                }
            }
        }
    }
}