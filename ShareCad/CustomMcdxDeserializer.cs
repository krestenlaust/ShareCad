using Ptc.PersistentData;
using System;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using Ptc.Controls.Core.Serialization;
using Ptc.IO;
using System.IO.Packaging;
using System.IO;
using Ptc.PersistentDataObjects;
using Ptc;
using Ptc.Serialization;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Documents;
using MindTouch.IO;
using Ptc.Xml;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    public class CustomMcdxDeserializer : IDeS11NProvider, IDisposable
    {
        public IList<IRegionPersistentData> DeserializedRegions { get; private set; }
        public bool SkipRegionsNotAffectingCalculation { get; set; }
        public ISerializationHelper Helper => this._serializationHelper;
        public XmlNamespaceManager NameSpaceManager => XmlWorksheetData.McdxNameSpaceManager;
        public XmlDocument XmlContentDocument => this._xmlContentDocument;
        public bool UseOverrides { get; private set; }
        public string FilePath => null;
        public PackagePart PackagePart => throw new NotImplementedException();

        private readonly ISerializationHelper _serializationHelper;
        private readonly IDeserializationStrategy _deserializationStrategy;
        private readonly IRegionCollectionSerializer _regionCollectionSerializer;
        private readonly string _worksheetNodeName;
        private readonly Stream _sourceStream;
        private XmlDocument _xmlContentDocument;

        public CustomMcdxDeserializer(Stream partStream, IDeserializationStrategy deserializationStrategy, ISerializationHelper serializationHelper, IRegionCollectionSerializer regionCollectionSerializer, bool useOverrides)
        {
            this._deserializationStrategy = deserializationStrategy;
            this._serializationHelper = serializationHelper;
            this._regionCollectionSerializer = regionCollectionSerializer;
            this._worksheetNodeName = regionCollectionSerializer.RootNodeName;
            this._sourceStream = partStream;
            this.UseOverrides = useOverrides;
        }

        public void Deserialize(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                this._xmlContentDocument = new XmlDocument();
                SpiritResolver.InitializeDocument(this._xmlContentDocument, SchemasManager.Worksheet50);
                this._xmlContentDocument.Load(stream);
                RepairXmlDocument(this._xmlContentDocument);
                this._xmlContentDocument.Validate(null);
                Deserialize(this._xmlContentDocument);
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
            if (this._serializationHelper.SerializedSchemaVersions != null && this._serializationHelper.SerializedSchemaVersions.ContainsKey(SchemasManager.SchemaNameEnum.Worksheet) && this._serializationHelper.SerializedSchemaVersions[SchemasManager.SchemaNameEnum.Worksheet] >= v)
            {
                return;
            }
            XmlNode xmlNode = SpiritResolver.WorksheetRootNode(xmlContentDocument, this.NameSpaceManager, this._worksheetNodeName);
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
            XmlNode xmlNode = SpiritResolver.WorksheetRootNode(xmlContentDocument, this.NameSpaceManager, this._worksheetNodeName);
            this._regionCollectionSerializer.Deserialize(xmlNode as XmlElement, this.UseOverrides);
            this.DeserializedRegions = this._deserializationStrategy.SetRegions(this._regionCollectionSerializer.Regions, this);
            this._deserializationStrategy.DeserializeRegionsEpilog(xmlContentDocument);
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

        public string UnpackExternalObject(string itemIdRef)
        {
            throw new NotImplementedException();

            /*
            //Stream sourceStream = PackageOperationsProvider.GetSourceStream(this.PackagePart, itemIdRef);

            string text;
            if (_sourceStream == null)
            {
                text = null;
            }
            else
            {
                //using (sourceStream)
                //{
                    text = Path.GetTempFileName();
                    using (FileStream fileStream = new FileStream(text, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        StreamUtils.CopyStream(_sourceStream, fileStream);
                    }
                //}
            }
            return text;*/
        }

        public Stream UnpackProtectedPart(string itemIdRef)
        {
            throw new NotImplementedException();

            /*
            ChunkedMemoryStream chunkedMemoryStream;
            //using (Stream sourceStream = PackageOperationsProvider.GetSourceStream(this.PackagePart, itemIdRef))
            //{
                chunkedMemoryStream = new ChunkedMemoryStream();
                SerializationHelperUtils.CopyObfuscatedStream(_sourceStream, chunkedMemoryStream, 16384);
            //}
            return chunkedMemoryStream;*/
        }

        public void ThrowFirstRegionDeserializationExceptionIfExists()
        {
            throw new NotImplementedException();

            /*
            Exception ex = this.DeserializedRegions.AllDeserializationExceptions().FirstOrDefault<Exception>();
            if (ex != null)
            {
                this.ThrowIncorrectSerializationFormatException(ex);
            }*/
        }

        public void Dispose()
        {
            this._xmlContentDocument = null;
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