using Ptc;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.ExcelComponent;
using Ptc.Controls.Include;
using Ptc.Controls.Whiteboard;
using Ptc.Controls.Worksheet;
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;
using Ptc.Undo;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Xml;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    public static class ManipulateWorksheet
    {
        private static Package _package;
        private static Package TempPackage
        {
            get
            {
                if (_package is null)
                {
                    // Generate temporary package.
                    string fileName = Path.GetTempPath() + Guid.NewGuid().ToString();
                    _package = Package.Open(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
                }

                return _package;
            }
        }

        public static void DeserializeAndApplySection(EngineeringDocument engineeringDocument, string xml)
        {
            var currentWorksheetData = engineeringDocument.Worksheet.GetWorksheetData();

            if (engineeringDocument is null)
            {
                return;
            }

            //WorksheetControl control = (WorksheetControl)sender;
            //var worksheetData = control.GetWorksheetData();

            var viewModel = ((WorksheetControl)engineeringDocument.Worksheet).GetViewModel();

            worksheetRegionCollectionSerializer regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            IWorksheetPersistentData worksheetData = new WorksheetPersistentData()
            {
                DisplayGrid = currentWorksheetData.DisplayGrid,
                DisplayHFGrid = currentWorksheetData.DisplayHFGrid,
                GridSize = currentWorksheetData.GridSize,
                LayoutSize = currentWorksheetData.LayoutSize,
                MarginType = currentWorksheetData.MarginType,
                OleObjectAutoResize = currentWorksheetData.OleObjectAutoResize,
                PageOrientation = currentWorksheetData.PageOrientation,
                PlotBackgroundType = currentWorksheetData.PlotBackgroundType,
                ShowIOTags = currentWorksheetData.ShowIOTags
            };

            using (CustomMcdxDeserializer mcdxDeserializer =
                new CustomMcdxDeserializer(
                    new CustomWorksheetSectionDeserializationStrategy(
                        worksheetData.WorksheetContent,
                        engineeringDocument.MathFormat,
                        engineeringDocument.LabeledIdFormat
                        ),
                    engineeringDocument.DocumentSerializationHelper,
                    regionCollectionSerializer,
                    true
                    )
                )
            {
                mcdxDeserializer.Deserialize(xml);
                var deserializedRegions = mcdxDeserializer.DeserializedRegions;
                
                engineeringDocument.DocumentSerializationHelper.MainRegions = deserializedRegions;

                ((WorksheetControl)engineeringDocument.Worksheet).ApplyWorksheetDataLite(worksheetData);
            }
        }

        public static XmlDocument SerializeRegions(IDictionary<UIElement, Point> serializableRegions, ISerializationHelper serializationHelper)
        {
            var regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            var mcdxSerializer = new CustomMcdxSerializer(
                new CustomWorksheetSectionSerializationStrategy(
                    serializableRegions,
                    () => new worksheetRegionType()
                    ),
                serializationHelper,
                regionCollectionSerializer,
                null,
                true);

            mcdxSerializer.Serialize();

            return mcdxSerializer.XmlContentDocument;
        }

        public static XmlDocument SerializeRegions(IDictionary<UIElement, Point> serializableRegions, EngineeringDocument engineeringDocument) =>
            SerializeRegions(serializableRegions, engineeringDocument.DocumentSerializationHelper);

        /*
        public static Stream SerializeRegions(EngineeringDocument engineeringDocument, IWorksheetSectionPersistentData serializableSection)
        {
            // Delete part if it already exists in tempPackage.
            var regionFileLocation = new Uri("/regions.xml", UriKind.Relative);
            TempPackage.DeletePart(regionFileLocation);

            var part = TempPackage.CreatePart(regionFileLocation, System.Net.Mime.MediaTypeNames.Text.Xml);

            MethodInfo dynMethod = engineeringDocument.GetType().GetMethod("SerializeWorksheetSection", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(engineeringDocument, new object[]
            {
                part,
                serializableSection,
                (DelegateFunction0<IRegionType>)(() => new worksheetRegionType()),
                new worksheetRegionCollectionSerializer()
            });

            return part.GetStream();
        }*/
    }
}
