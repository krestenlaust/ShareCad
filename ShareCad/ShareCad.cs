using HarmonyLib;
using Microsoft.Win32.SafeHandles;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using Ptc.Wpf;
using Ptc.PersistentData;
using Spirit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Xml.Serialization;
using Ptc.Controls.Whiteboard;
using System.Windows.Input;
using Ptc.FunctionalitiesLimitation;

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

        public void ShareCadInit()
        {
            if (!initialised)
            {
                var harmony = new Harmony("ShareCad");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                WinConsole.Initialize();
                /*
                var messageBoxResult = MessageBox.Show("Host?", 
                    "ShareCad", 
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, 
                    MessageBoxResult.Cancel, 
                    MessageBoxOptions.DefaultDesktopOnly
                    );

                switch (messageBoxResult)
                {
                    case MessageBoxResult.Yes:
                        // stuff
                        break;

                    case MessageBoxResult.No:
                        // stuff
                        break;

                    default:
                        break;
                }*/
                initialised = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })] // Fra MathcadPrime.exe
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            engineeringDocument = (EngineeringDocument)__result;
        }

        private static void EngineeringDocument_QueryCursor(object sender, System.Windows.Input.QueryCursorEventArgs e)
        {
            Console.WriteLine($"Cursor moved");
        }

        static bool subscribed = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action)
        {
            if (engineeringDocument is null)
            {
                return;
            }

            if (!subscribed)
            {
                engineeringDocument.Worksheet.PropertyChanged += Worksheet_PropertyChanged;
                subscribed = true;
            }

            #region unused
            //FileLoadResult fileLoadResult = new FileLoadResult();

            //engineeringDocument.OpenPackage(ref fileLoadResult, @"C:\Users\kress\Documents\Debug.mcdx", false);

            /*
            var worksheet = engineeringDocument.WorksheetData;
            if (worksheet is null)
            {
                Console.WriteLine("Træls");
                return;
            }

            var content = worksheet.WorksheetContent;
            if (content is null)
            {
                Console.WriteLine("Træls2");
                return;
            }

            if (content is null || content.SerializedRegions is null)
            {
                Console.WriteLine("Null?");
                return;
            }

            Console.WriteLine(content.SerializedRegions.Count);

            foreach (var item in content.SerializedRegions)
            {
                Console.WriteLine(item.RegionData.Item);
            }*/
            #endregion
        }

        private static bool registeredKeys;

        private static void Worksheet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // register key inputs.
            if (!registeredKeys)
            {
                CommandManager.RegisterClassInputBinding(
                    typeof(WorksheetControl), 
                    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));
                registeredKeys = true;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"=== {DateTime.Now:HH:mm:ss} - PropertyChange invoked - {e.PropertyName} ===");
            Console.ForegroundColor = ConsoleColor.Gray;

            WorksheetControl control = (WorksheetControl)sender;
            var worksheetData = control.GetWorksheetData();

            var viewModel = control.GetViewModel();

            Console.WriteLine($" - ActiveItem: {control.ActiveItem}, {control.ActiveDescendant}, {control.CurrentElement}");
            // for at finde ud af hvad der gør dem unik så man kan sende et ID med over nettet.
            Console.WriteLine($" - Name: {control.Name} Id: {control.PersistId} Uid: {control.Uid}");

            // Liste over aktive elementer.
            Console.WriteLine(" - Active section items:");
            foreach (var item in viewModel.ActiveSection.WhiteboardItems)
            {
                Console.WriteLine($"{item}");
            }

            Console.WriteLine();

            switch (e.PropertyName)
            {
                case "SelectedDescendants":
                    // finder det første element lavet, eller null.
                    var firstElement = control.ActiveSectionItems.FirstOrDefault();

                    // aktivér debug test scenarie hvis der laves en tekstboks som det første element.
                    if (firstElement is Ptc.Controls.Text.TextRegion realText)
                    {
                        // flyt det første element til koordinatet (0, 5)
                        control.MoveItemGridLocation(firstElement, new Point(0, 5));

                        Console.WriteLine("Moved control to 0, 5");

                        // Prøv at oprette et tekst element, (der bliver ikke gjort mere ved det lige nu).
                        Ptc.Controls.Text.TextRegion textRegion = new Ptc.Controls.Text.TextRegion()
                        {
                            Text = "INJECTED!",
                        };

                        //var res = AnyDiff.AnyDiff.Diff(realText, textRegion);

                        // Indsæt tekst element.
                        // ???
                        viewModel.AddItemAtLocation(textRegion, viewModel.GridLocationToWorksheetLocation(new Point(5, 5)));
                        //var worksheetData = control.GetWorksheetData();
                        if (worksheetData is null)
                        {
                            break;
                        }
                        
                        // Profit! (andre test ting)

                    }
                    break;
                case "CurrentElement":
                    /* // ingen grund til at kigge på de her ting mere, siden man kan se dem under control.
                    IWorksheetPersistentData worksheetData = control.GetWorksheetData();

                    if (worksheetData is null)
                    {
                        break;
                    }

                    var regions = worksheetData.WorksheetContent.RegionsToSerialize;

                    foreach (var item in regions)
                    {
                        Console.WriteLine(item.Key.GetType() + ":" + item.Value);
                    }*/
                    break;
                case "WorksheetPageLayoutMode":
                    // changed from draft to page
                    break;
                default:
                    break;
            }
        }
    }
}