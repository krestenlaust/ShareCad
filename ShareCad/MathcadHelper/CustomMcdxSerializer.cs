using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Schema;
using Ptc;
using Ptc.Controls.Core.Serialization;
using Ptc.IO;
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;
using Ptc.Wpf;
using Ptc.Xml;

namespace ShareCad
{
    public class CustomMcdxSerializer : IS11NProvider, IBaseS11NProvider
	{
		private readonly StoreAsset SetAsset;
		public delegate void StoreAsset(int ID, string filePath);

		private readonly Action<XmlSchemaException> _onWriteValidationFailed;
		private readonly ISerializationHelper _serializationHelper;
		private readonly PackagePart _packagePart;

		public XmlDocument XmlContentDocument { get; private set; }

		public bool UseOverrides { get; private set; }

		public PackagePart PackagePart
		{
			get
			{
				return _packagePart;
			}
		}

		public ISerializationHelper Helper
		{
			get
			{
				return _serializationHelper;
			}
		}

		public XmlNamespaceManager NameSpaceManager
		{
			get
			{
				return XmlWorksheetData.McdxNameSpaceManager;
			}
		}

		public CustomMcdxSerializer(
			ISerializationHelper helper,
			StoreAsset storeAsset
            )
        {
			UseOverrides = true;
			_serializationHelper = helper;
			_onWriteValidationFailed = null;

			SetAsset = storeAsset;
        }

		/*
		public CustomMcdxSerializer(
			ISerializationStrategy serializationStrategy, 
			ISerializationHelper helper, 
			IRegionCollectionSerializer regionCollectionSerializer, 
			Action<XmlSchemaException> onWriteValidationFailed, 
			bool useOverrides,
			StoreAsset storeAsset)
		{
			_serializationStrategy = serializationStrategy;
			_worksheetNodeName = regionCollectionSerializer.RootNodeName;
			_regionCollectionSerializer = regionCollectionSerializer;
			_serializationHelper = helper;
			_onWriteValidationFailed = onWriteValidationFailed;
			UseOverrides = useOverrides;

			SetAsset = storeAsset;
		}*/

		public void Serialize(IRegionCollectionSerializer regionCollectionSerializer, ISerializationStrategy serializationStrategy)
		{
			XmlContentDocument = new XmlDocument();

			SpiritResolver.InitializeDocument(XmlContentDocument, SchemasManager.Worksheet50);

			BuildDocumentXml(regionCollectionSerializer, serializationStrategy);
			XmlContentDocument.Validate(new ValidationEventHandler(OnWriteValidationFailedHandler));
		}

		private void OnWriteValidationFailedHandler(object sender, ValidationEventArgs args)
        {
            if (_onWriteValidationFailed == null)
            {
                throw args.Exception;
            }

            _onWriteValidationFailed(args.Exception);
        }

        public static string PackImage(PackagePart sourcePackagePart, ISerializationHelper serializationHelper, Uri imageSourceUri, byte[] imageSourceStreamHash, BitmapSource image, string uniqueImageName, string imageExtension)
		{
			if (sourcePackagePart == null)
			{
				throw new NullReferenceException("package part doesn't exist");
			}

			byte[] imageHash = ImageUtils.GetImageHash(image);
			Func<Pair<string, Uri>> getExistingPartRelationshipId = () => serializationHelper.GetImagePath(imageExtension, imageHash);
			Action<string, PackagePart> cacheRelationshipIdForNewPart = delegate (string relationshipId, PackagePart imagePart)
			{
				serializationHelper.AddImagePath(imageExtension, imageHash, new Pair<string, Uri>(relationshipId, imagePart.Uri));
			};
			Func<PackagePart> writeNewPartToPackage = () => PackageOperationsProvider.CreateNewImagePart(sourcePackagePart, image, imageExtension, imageSourceStreamHash, uniqueImageName, imageSourceUri);
			return PackageOperationsProvider.PackPartAndGetId(sourcePackagePart, getExistingPartRelationshipId, cacheRelationshipIdForNewPart, writeNewPartToPackage, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image");
		}

		public static string PackFile(PackagePart sourcePackagePart, string sourceFileName, string pathInPackage, string relationshipUri, string mimeType)
		{
			if (sourcePackagePart == null)
			{
				throw new NullReferenceException("package part doesn't exist");
			}

			PackagePart packagePart = PackageOperationsProvider.Instance.AddPartToPackage(sourcePackagePart.Package, pathInPackage, mimeType);
			using (Stream stream = packagePart.GetStream(FileMode.Create))
			{
				using (FileStream fileStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read))
				{
					StreamUtils.CopyStream(fileStream, stream);
				}
			}
			PackageRelationship packageRelationship = sourcePackagePart.CreateRelationship(packagePart.Uri, TargetMode.Internal, relationshipUri);
			return packageRelationship.Id;
		}

		public string PackImage(ImagePackagingInformation pictureInfo)
		{
			return PackImage(_packagePart, _serializationHelper, pictureInfo.ImageSourceUri, pictureInfo.ImageOriginalStreamHash, pictureInfo.Image, pictureInfo.ImageName, pictureInfo.ImageExtension);
		}

		public string PackExternalObject(string sourceFileName, string objectUniqueId, ExternalObjectType objectType)
		{
			string format;
			string mimeType;
			string relationshipUri;
			switch (objectType)
			{
				case ExternalObjectType.Excel:
					format = "/mathcad/excel/excel{0}.ole";
					mimeType = "application/vnd.openxmlformats-officedocument.oleObject";
					relationshipUri = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/excel";
					break;
				case ExternalObjectType.Ole:
					format = "/mathcad/ole/object{0}.ole";
					mimeType = "application/vnd.openxmlformats-officedocument.oleObject";
					relationshipUri = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject";
					break;
				default:
					throw new NotImplementedException("Unsupported external object type: " + objectType);
			}
			return PackFile(_packagePart, sourceFileName, string.Format(format, objectUniqueId), relationshipUri, mimeType);
		}

		public string PackIncludedWorksheet(string fullFilePath, DateTime lastSavedDate, string uniqueId, Func<Package, string, PackagePart> writePartToPackage)
		{
			Pair<string, DateTime> fullFilePathAndTime = new Pair<string, DateTime>(fullFilePath, lastSavedDate);
			Func<Pair<string, Uri>> getExistingPartRelationshipId = () => Helper.GetCachedWorksheetPath(fullFilePathAndTime);
			Action<string, PackagePart> cacheRelationshipIdForNewPart = delegate (string relationshipId, PackagePart cachedPart)
			{
				Helper.AddCachedPartPath(fullFilePathAndTime, new Pair<string, Uri>(relationshipId, cachedPart.Uri));
			};

			Func<PackagePart> writeNewPartToPackage = () => writePartToPackage(PackagePart.Package, uniqueId);
			return PackageOperationsProvider.PackPartAndGetId(PackagePart, getExistingPartRelationshipId, cacheRelationshipIdForNewPart, writeNewPartToPackage, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/include");
		}

		public string PackFlowDocument(FlowDocument flowDocument, string regionId)
		{
			string tempFile = Path.GetTempFileName();
            using (Stream stream = File.Open(tempFile, FileMode.Create, FileAccess.ReadWrite))
            {
				TextRegionSerializationHelper.GetTextRangeFromStartToEnd(flowDocument).Save(stream, DataFormats.XamlPackage);
				stream.Flush();
			}

			SetAsset(int.Parse(regionId), tempFile);

			return regionId;
		}

		public string PackProtectedPart(Stream content, string uniqueId, string regionType)
		{
			PackagePart packagePart = PackagePart;
			PackagePart packagePart2 = PackageOperationsProvider.Instance.AddPartToPackage(packagePart.Package, string.Format("/mathcad/ProtectedData/{0}{1}.dat", regionType, uniqueId), "application/octet-stream");
			PackageRelationship packageRelationship = packagePart.CreateRelationship(packagePart2.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/ProtectedData");
			using (Stream stream = packagePart2.GetStream(FileMode.Create, FileAccess.ReadWrite))
			{
				SerializationHelperUtils.CopyObfuscatedStream(content, stream, 16384);
				stream.Flush();
			}
			return packageRelationship.Id;
		}

		private void BuildDocumentXml(IRegionCollectionSerializer regionCollectionSerializer, ISerializationStrategy serializationStrategy)
		{
			IList<IRegionType> regions = serializationStrategy.GetRegions(this);
			regions.Each(delegate (IRegionType item)
			{
				if (item.S11NProvider == null)
				{
					item.S11NProvider = this;
					return;
				}

				IS11NProvider s11NProvider = item.S11NProvider;
				while (s11NProvider is IHasS11NProvider)
				{
					s11NProvider = (s11NProvider as IHasS11NProvider).S11NProvider;
				}
			});
			regionCollectionSerializer.Regions = regions;
			regionCollectionSerializer.Serialize(XmlContentDocument, UseOverrides);
			serializationStrategy.SerializeRegionsEpilog(XmlContentDocument);
            
			if (SpiritResolver.WorksheetRootNode(XmlContentDocument, NameSpaceManager, regionCollectionSerializer.RootNodeName) is XmlElement xmlElement)
            {
                xmlElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                xmlElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                xmlElement.SetAttribute("xmlns:ve", "http://schemas.openxmlformats.org/markup-compatibility/2006");
                xmlElement.SetAttribute("xmlns:r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                xmlElement.SetAttribute("xmlns:ws", "http://schemas.mathsoft.com/worksheet50");
                xmlElement.SetAttribute("xmlns:ml", "http://schemas.mathsoft.com/math50");
                xmlElement.SetAttribute("xmlns:u", "http://schemas.mathsoft.com/units10");
                xmlElement.SetAttribute("xmlns:p", "http://schemas.mathsoft.com/provenance10");
            }
        }
	}
}
