using HarmonyLib;
using Microsoft.Win32.SafeHandles;
using Ptc.Controls;
using Ptc.Controls.Text;
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
using System.IO.Packaging;
using System.IO;
using Ptc.PersistentDataObjects;
using Ptc;
using Ptc.Serialization;
using System.Collections.Generic;
using System.Net;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    // bliver ikke brugt til noget.
    internal static class ModuleInitializer
    {
        internal static void Run()
        {

        }
    }

    [HarmonyPatch]
    public class ShareCad
    {
        static EngineeringDocument engineeringDocument;
        static bool initializedModule = false;

        public void ShareCadInit()
        {
            if (initializedModule)
            {
                return;
            }

            var harmony = new Harmony("ShareCad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            WinConsole.Initialize();

            Console.WriteLine("LOADED!");
            initializedModule = true;
        }

        // Fra MathcadPrime.exe
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })]
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            engineeringDocument = __result as EngineeringDocument;
            Console.WriteLine("Retrieved document instance");
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
                engineeringDocument.MouseDoubleClick += EngineeringDocument_MouseDoubleClick;
                subscribed = true;
            }

            //FileLoadResult fileLoadResult = new FileLoadResult();

            //engineeringDocument.OpenPackage(ref fileLoadResult, @"C:\Users\kress\Documents\Debug.mcdx", false);
        }

        private static void EngineeringDocument_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Double click!");

            WorksheetControl control = (WorksheetControl)((EngineeringDocument)sender).Content;
            var worksheetData = control.GetWorksheetData();

            if (worksheetData is null)
            {
                return;
            }

            using (Stream xmlStream = SerializeRegions(worksheetData.WorksheetContent))
            {
                Networking.Networking.TransmitStream(xmlStream);
            }
        }

        private static bool initializedTests;
        private static Package tempPackage;
        
        private static void Worksheet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // register key inputs and other.
            if (!initializedTests)
            {
                initializedTests = true;

                // debug other stuff.
                //Networking.Networking.SendObject();

                string fileName = Path.GetTempPath() + Guid.NewGuid().ToString();

                tempPackage = Package.Open(fileName, FileMode.CreateNew, FileAccess.ReadWrite);

                // register keys
                CommandManager.RegisterClassCommandBinding(
                    typeof(WorksheetControl),
                    new CommandBinding(
                        WorksheetCommands.NewSolveBlock,
                        (o, localE) => SyncroniseExecuted(o, localE),
                        (_, localE) => { localE.CanExecute = true; }
                    ));

                /*
                CommandManager.RegisterClassInputBinding(
                    typeof(WorksheetControl), 
                    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));*/
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
                    if (firstElement is TextRegion realText)
                    {
                        Console.WriteLine("First element is text");

                        #region Testing
                        // flyt det første element til koordinatet (0, 5)
                        //control.MoveItemGridLocation(firstElement, new Point(0, 2));

                        //realText.Text = "👑〖⚡ᖘ๖ۣۜℜΘ𝕵ECT ΘVERRIDE⚡〗👑";

                        /*
                        // Prøv at oprette et tekst element, (der bliver ikke gjort mere ved det lige nu).
                        Ptc.Controls.Text.TextRegion textRegion = new Ptc.Controls.Text.TextRegion()
                        {
                            Text = "INJECTED!",
                        };

                        // Indsæt tekst element.
                        viewModel.AddItemAtLocation(textRegion, viewModel.GridLocationToWorksheetLocation(new Point(5, 7)));
                        */
                        #endregion

                        if (worksheetData is null)
                        {
                            break;
                        }

                        // Profit! (andre test ting)
                        using (Stream xmlStream = SerializeRegions(worksheetData.WorksheetContent))
                        {
                            Networking.Networking.TransmitStream(xmlStream);
                        }

                        //TcpClient client = new TcpClient("192.168.2.215", 8080);
                        //var tcpStream = client.GetStream();
                        Networking.Networking.Server.BindListener(IPAddress.Loopback);
                    }
                    else if (firstElement is SolveBlockControl solveBlock)
                    {
                        Console.WriteLine("First element is solveblock");
                        Networking.Networking.Client.Connect(IPAddress.Loopback);
                    }
                    break;
                case "CurrentElement":
                    break;
                case "WorksheetPageLayoutMode":
                    // changed from draft to page
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Et forsøg på at have en event ting.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private static void SyncroniseExecuted(object target, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void TransferStream(Stream source, Stream target)
        {
            int currentByte = source.ReadByte();
            while (currentByte != -1)
            {
                target.WriteByte((byte)currentByte);
                currentByte = source.ReadByte();
            }
        }

        private static List<IRegionPersistentData> DeserializeSection(Stream serializedSection, IWorksheetSectionPersistentData sectionData)
        {
            #region old
            //worksheetRegionCollectionSerializer regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            // deserialize regions
            // DeserializeWorksheetSection(this.PackageOpsProvider.GetMainPartFromPackage(package), this.WorksheetData.WorksheetContent, worksheetRegionCollectionSerializer, fullFilePath);
            // DeserializeWorksheetSection(PackagePart, IWorksheetSectionPersistentData, IRegionCollectionSerializer, string);

            /*
            MethodInfo dynMethod = engineeringDocument.GetType().GetMethod("DeserializeWorksheetSection", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(engineeringDocument, new object[]
            {
                deserializeableSection,
                worksheetContent,
                worksheetRegionCollectionSerializer,
                ""
            });*/
            #endregion

            worksheetRegionCollectionSerializer regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            using (CustomMcdxDeserializer mcdxDeserializer =
                new CustomMcdxDeserializer(
                    serializedSection,
                    new CustomWorksheetSectionDeserializationStrategy(
                        sectionData,
                        engineeringDocument.MathFormat,
                        engineeringDocument.LabeledIdFormat
                        ),
                    engineeringDocument.DocumentSerializationHelper,
                    regionCollectionSerializer,
                    true
                    )
                )
            {
                mcdxDeserializer.Deserialize(serializedSection);
                return (List<IRegionPersistentData>)mcdxDeserializer.DeserializedRegions;
            }
        }

        private static Stream SerializeRegions(IWorksheetSectionPersistentData serializableSection)
        {
            // Delete part if it already exists in tempPackage.
            var regionFileLocation = new Uri("/regions.xml", UriKind.Relative);
            tempPackage.DeletePart(regionFileLocation);

            var part = tempPackage.CreatePart(regionFileLocation, System.Net.Mime.MediaTypeNames.Text.Xml);

            MethodInfo dynMethod = engineeringDocument.GetType().GetMethod("SerializeWorksheetSection", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(engineeringDocument, new object[]
            {
                part,
                serializableSection,
                (DelegateFunction0<IRegionType>)(() => new worksheetRegionType()),
                new worksheetRegionCollectionSerializer()
            });

            return part.GetStream();
        }
    }
}

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