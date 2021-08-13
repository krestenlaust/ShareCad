using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using Ptc.Controls.Core.Serialization;
using Ptc.PersistentData;
using Ptc.Serialization;
using Ptc.Xml;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    public class CustomMcdxDeserializer : IDeS11NProvider, IDisposable
    {
        public IList<IRegionPersistentData> DeserializedRegions { get; private set; }
        public bool SkipRegionsNotAffectingCalculation { get; set; }
        public ISerializationHelper Helper => _serializationHelper;
        public XmlNamespaceManager NameSpaceManager => XmlWorksheetData.McdxNameSpaceManager;
        public XmlDocument XmlContentDocument => _xmlContentDocument;
        public bool UseOverrides { get; private set; }
        public string FilePath => null;
        public PackagePart PackagePart => throw new NotImplementedException();

        private readonly ISerializationHelper _serializationHelper;
        private readonly IDeserializationStrategy _deserializationStrategy;
        private readonly IRegionCollectionSerializer _regionCollectionSerializer;
        private readonly string _worksheetNodeName;
        private readonly Stream _sourceStream;
        private XmlDocument _xmlContentDocument;

        public CustomMcdxDeserializer(IDeserializationStrategy deserializationStrategy, ISerializationHelper serializationHelper, IRegionCollectionSerializer regionCollectionSerializer, bool useOverrides)
        {
            _deserializationStrategy = deserializationStrategy;
            _serializationHelper = serializationHelper;
            _regionCollectionSerializer = regionCollectionSerializer;
            _worksheetNodeName = regionCollectionSerializer.RootNodeName;
            UseOverrides = useOverrides;
        }

        public void Deserialize(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                _xmlContentDocument = new XmlDocument();
                SpiritResolver.InitializeDocument(_xmlContentDocument, SchemasManager.Worksheet50);
                _xmlContentDocument.Load(stream);
                RepairXmlDocument(_xmlContentDocument);
                _xmlContentDocument.Validate(null);
                Deserialize(_xmlContentDocument);
            }
            catch (Exception ex)
            {
                ThrowIncorrectSerializationFormatException(ex);
            }
        }

        public void Deserialize(string xml)
        {
            try
            {
                _xmlContentDocument = new XmlDocument();
                SpiritResolver.InitializeDocument(_xmlContentDocument, SchemasManager.Worksheet50);
                
                _xmlContentDocument.LoadXml(xml);
                RepairXmlDocument(_xmlContentDocument);
                _xmlContentDocument.Validate(null);
                
                Deserialize(_xmlContentDocument);
            }
            catch (Exception ex)
            {
                ThrowIncorrectSerializationFormatException(ex);
            }
        }

        private void RepairXmlDocument(XmlDocument xmlContentDocument) => RepairPictureElements(xmlContentDocument);

        private void RepairPictureElements(XmlDocument xmlContentDocument)
        {
            Version v = new Version("5.2.10");

            if (_serializationHelper.SerializedSchemaVersions != null && _serializationHelper.SerializedSchemaVersions.ContainsKey(SchemasManager.SchemaNameEnum.Worksheet) && _serializationHelper.SerializedSchemaVersions[SchemasManager.SchemaNameEnum.Worksheet] >= v)
            {
                return;
            }

            XmlNode xmlNode = SpiritResolver.WorksheetRootNode(xmlContentDocument, NameSpaceManager, _worksheetNodeName);
            foreach (object obj in xmlNode.FirstChild.ChildNodes)
            {
                XmlNode xmlNode2 = (XmlNode)obj;
                if (xmlNode2.FirstChild != null && xmlNode2.FirstChild.Name.Equals("picture"))
                {
                    RepairPictureElementIfNeed(xmlNode2);
                }
            }
        }

        protected void ThrowIncorrectSerializationFormatException(Exception ex) => throw new FormatException("Incorrect serialization format", ex);

        public void Deserialize(XmlDocument xmlContentDocument)
        {
            XmlNode xmlNode = SpiritResolver.WorksheetRootNode(xmlContentDocument, NameSpaceManager, _worksheetNodeName);
            _regionCollectionSerializer.Deserialize(xmlNode as XmlElement, UseOverrides);
            DeserializedRegions = _deserializationStrategy.SetRegions(_regionCollectionSerializer.Regions, this);
            _deserializationStrategy.DeserializeRegionsEpilog(xmlContentDocument);
        }

        public void CustomUnpackFlowDocument(ref FlowDocument flowDocument, Stream sourceStream)
        {
            if (flowDocument is null)
            {
                flowDocument = new FlowDocument();
            }

            TextRange textRangeFromStartToEnd = TextRegionSerializationHelper.GetTextRangeFromStartToEnd(flowDocument);
            textRangeFromStartToEnd.Load(sourceStream, DataFormats.XamlPackage);
        }

        public void UnpackFlowDocument(ref FlowDocument flowDocument, string itemIdRef)
        {
            //Stream sourceStream = PackageOperationsProvider.GetSourceStream(this.PackagePart, itemIdRef);
            if (_sourceStream != null)
            {
                //using (sourceStream)
                //{
                if (flowDocument == null)
                {
                    flowDocument = new FlowDocument();
                }
                System.Windows.Documents.TextRange textRangeFromStartToEnd = TextRegionSerializationHelper.GetTextRangeFromStartToEnd(flowDocument);
                textRangeFromStartToEnd.Load(_sourceStream, DataFormats.XamlPackage);
                //}
            }
        }

        private static void RepairPictureElementIfNeed(XmlNode region)
        {
            throw new NotImplementedException();

            /*
            XmlNode firstChild = region.FirstChild;
            ItemChoiceType itemChoiceType;
            if (firstChild.FirstChild != null && !Enum.TryParse<ItemChoiceType>(firstChild.FirstChild.Name, out itemChoiceType))
            {
                XmlAttributeCollection attributes = firstChild.FirstChild.Attributes;
                if (DebugTools.AssertNotNull(firstChild.OwnerDocument, null, new object[0]).Passed)
                {
                    XmlElement xmlElement = firstChild.OwnerDocument.CreateElement("ws", ItemChoiceType.unknown.ToString(), "http://schemas.mathsoft.com/worksheet50");
                    if (DebugTools.AssertNotNull(attributes, null, new object[0]).Passed)
                    {
                        foreach (object obj in attributes)
                        {
                            XmlAttribute xmlAttribute = (XmlAttribute)obj;
                            xmlElement.SetAttribute(xmlAttribute.Name, xmlAttribute.Value);
                        }
                    }
                    firstChild.ReplaceChild(xmlElement, firstChild.FirstChild);
                }
            }*/
        }

        public string UnpackExternalObject(string itemIdRef) => throw new NotImplementedException();

        public Stream UnpackProtectedPart(string itemIdRef) => throw new NotImplementedException();

        public void ThrowFirstRegionDeserializationExceptionIfExists() => throw new NotImplementedException();

        public void Dispose() => _xmlContentDocument = null;
    }
}
