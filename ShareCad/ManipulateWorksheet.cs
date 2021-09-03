using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Xml;
using Ptc.Controls;
using Ptc.Controls.Worksheet;
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    public static class ManipulateWorksheet
    {
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
