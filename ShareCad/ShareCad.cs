using HarmonyLib;
using Microsoft.Win32.SafeHandles;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using Ptc.Wpf;
using Ptc.PersistentData;
using Spirit;
using System;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using Ptc.Controls.Whiteboard;
using System.Windows.Input;
using Ptc.FunctionalitiesLimitation;
using Networking;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    internal static class ModuleInitializer
    {
        internal static void Run()
        {
            Console.WriteLine("LOADED!");

            var harmony = new Harmony("ShareCad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //WinConsole.Initialize();
            
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
        }
    }

    [HarmonyPatch]
    public class ShareCad
    {
        static EngineeringDocument engineeringDocument;

        // Fra MathcadPrime.exe
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })]
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            engineeringDocument = __result as EngineeringDocument;
            Console.WriteLine("Result: " + engineeringDocument);
        }

        private static void EngineeringDocument_QueryCursor(object sender, QueryCursorEventArgs e)
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

        private static bool initialized;
        
        private static void Worksheet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // register key inputs and other.
            if (!initialized)
            {
                initialized = true;

                // debug other stuff.
                Networking.Models.TextRegionDto dto = new Networking.Models.TextRegionDto()
                {
                    ID = "Kresten",
                    GridPosition = new Point(5, 5),
                    TextContent = "Kresten",
                };
                
                Networking.Networking.SendObject(dto);

                // register keys
                CommandManager.RegisterClassInputBinding(
                    typeof(WorksheetControl), 
                    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"=== {DateTime.Now:HH:mm:ss} - PropertyChange invoked - {e.PropertyName} ===");
            Console.ForegroundColor = ConsoleColor.Gray;

            WorksheetControl control = (WorksheetControl)sender;
            var worksheetData = control.GetWorksheetData();

            var viewModel = control.GetViewModel();

            //Console.WriteLine($" - ActiveItem: {control.ActiveItem}, {control.ActiveDescendant}, {control.CurrentElement}");
            // for at finde ud af hvad der gør dem unik så man kan sende et ID med over nettet.
            Console.WriteLine($"ID: {control.PersistId}");

            // Liste over aktive elementer.
            Console.WriteLine(" - Active section items:");
            foreach (var item in worksheetData.WorksheetContent.RegionsToSerialize)
            {
                Console.WriteLine($"{item.Key}");
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
                        //control.MoveItemGridLocation(firstElement, new Point(0, 2));

                        // 👑〖⚡ᖘ๖ۣۜℜΘ𝕵ECT ΘVERRIDE⚡〗👑
                        realText.Text = "👑〖⚡ᖘ๖ۣۜℜΘ𝕵ECT ΘVERRIDE⚡〗👑";

                        Console.WriteLine("Moved control to 0, 5");

                        // Prøv at oprette et tekst element, (der bliver ikke gjort mere ved det lige nu).
                        Ptc.Controls.Text.TextRegion textRegion = new Ptc.Controls.Text.TextRegion()
                        {
                            Text = "INJECTED!",
                        };

                        //var res = AnyDiff.AnyDiff.Diff(realText, textRegion);

                        // Indsæt tekst element.
                        // ???
                        viewModel.AddItemAtLocation(textRegion, viewModel.GridLocationToWorksheetLocation(new Point(5, 7)));
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