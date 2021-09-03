using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Schema;
using Ptc.IO;
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;
using Ptc.Wpf;
using Ptc.Xml;
using Ptc.Controls.Core.Serialization;
using Ptc;

namespace ShareCad.MathcadHelper
{
	public class McdxSerializer : IS11NProvider, IBaseS11NProvider
	{
		public McdxSerializer(PackagePart part, ISerializationStrategy serializationStrategy, ISerializationHelper helper, IRegionCollectionSerializer regionCollectionSerializer, Action<XmlSchemaException> onWriteValidationFailed, bool useOverrides)
		{
			if (part == null)
			{
				throw new ArgumentNullException("part");
			}
			this._serializationStrategy = serializationStrategy;
			this._worksheetNodeName = regionCollectionSerializer.RootNodeName;
			this._regionCollectionSerializer = regionCollectionSerializer;
			this._serializationHelper = helper;
			this._packagePart = part;
			this._onWriteValidationFailed = onWriteValidationFailed;
			this.UseOverrides = useOverrides;
		}

		public void Serialize(PackagePart part, bool useTempFile = false)
		{
			XmlContentDocument = new XmlDocument();

			SpiritResolver.InitializeDocument(XmlContentDocument, SchemasManager.Worksheet50);

			BuildDocumentXml(_regionCollectionSerializer);
			XmlContentDocument.Validate(new ValidationEventHandler(OnWriteValidationFailedHandler));

			//

			if (useTempFile)
			{
				SerializationHelperUtils.SaveToPartUsingTemporaryFile(delegate (string tempFile)
				{
					this.XmlContentDocument.Save(tempFile);
				}, part, null);
				return;
			}

			using (Stream stream = part.GetStream(FileMode.Create))
			{
				this.XmlContentDocument.Save(stream);
			}
		}

		private void OnWriteValidationFailedHandler(object sender, ValidationEventArgs args)
		{
			if (this._onWriteValidationFailed != null)
			{
				this._onWriteValidationFailed(args.Exception);
				return;
			}
			throw args.Exception;
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
			return McdxSerializer.PackImage(this._packagePart, this._serializationHelper, pictureInfo.ImageSourceUri, pictureInfo.ImageOriginalStreamHash, pictureInfo.Image, pictureInfo.ImageName, pictureInfo.ImageExtension);
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
			return McdxSerializer.PackFile(this._packagePart, sourceFileName, string.Format(format, objectUniqueId), relationshipUri, mimeType);
		}

		public string PackIncludedWorksheet(string fullFilePath, DateTime lastSavedDate, string uniqueId, Func<Package, string, PackagePart> writePartToPackage)
		{
			DebugTools.DevAssert(!string.IsNullOrEmpty(fullFilePath), null, new object[0]);
			Pair<string, DateTime> fullFilePathAndTime = new Pair<string, DateTime>(fullFilePath, lastSavedDate);
			Func<Pair<string, Uri>> getExistingPartRelationshipId = () => this.Helper.GetCachedWorksheetPath(fullFilePathAndTime);
			Action<string, PackagePart> cacheRelationshipIdForNewPart = delegate (string relationshipId, PackagePart cachedPart)
			{
				this.Helper.AddCachedPartPath(fullFilePathAndTime, new Pair<string, Uri>(relationshipId, cachedPart.Uri));
			};
			Func<PackagePart> writeNewPartToPackage = () => writePartToPackage(this.PackagePart.Package, uniqueId);
			return PackageOperationsProvider.PackPartAndGetId(this.PackagePart, getExistingPartRelationshipId, cacheRelationshipIdForNewPart, writeNewPartToPackage, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/include");
		}

		public XmlDocument XmlContentDocument { get; private set; }

		public bool UseOverrides { get; private set; }

		public PackagePart PackagePart
		{
			get
			{
				return this._packagePart;
			}
		}

		public ISerializationHelper Helper
		{
			get
			{
				return this._serializationHelper;
			}
		}

		public XmlNamespaceManager NameSpaceManager
		{
			get
			{
				return XmlWorksheetData.McdxNameSpaceManager;
			}
		}

		public string PackFlowDocument(FlowDocument flowDocument, string regionId)
		{
			PackagePart packagePart = this.PackagePart;
			PackagePart packagePart2 = PackageOperationsProvider.Instance.AddPartToPackage(packagePart.Package, string.Format("/mathcad/xaml/FlowDocument{0}.{1}", regionId, DataFormats.XamlPackage), "application/zip");
			PackageRelationship packageRelationship = packagePart.CreateRelationship(packagePart2.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/flowDocument");
			using (Stream stream = packagePart2.GetStream(FileMode.Create, FileAccess.ReadWrite))
			{
				TextRegionSerializationHelper.SaveTextRangeAsXamlPackage(TextRegionSerializationHelper.GetTextRangeFromStartToEnd(flowDocument), stream);
				stream.Flush();
			}
			return packageRelationship.Id;
		}

		public string PackProtectedPart(Stream content, string uniqueId, string regionType)
		{
			PackagePart packagePart = this.PackagePart;
			PackagePart packagePart2 = PackageOperationsProvider.Instance.AddPartToPackage(packagePart.Package, string.Format("/mathcad/ProtectedData/{0}{1}.dat", regionType, uniqueId), "application/octet-stream");
			PackageRelationship packageRelationship = packagePart.CreateRelationship(packagePart2.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/ProtectedData");
			using (Stream stream = packagePart2.GetStream(FileMode.Create, FileAccess.ReadWrite))
			{
				SerializationHelperUtils.CopyObfuscatedStream(content, stream, 16384);
				stream.Flush();
			}
			return packageRelationship.Id;
		}

		private void BuildDocumentXml(IRegionCollectionSerializer regionCollectionSerializer)
		{
			IList<IRegionType> regions = this._serializationStrategy.GetRegions(this);
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
				DebugTools.DevAssert(s11NProvider == this, null, new object[0]);
			});
			regionCollectionSerializer.Regions = regions;
			regionCollectionSerializer.Serialize(this.XmlContentDocument, this.UseOverrides);
			this._serializationStrategy.SerializeRegionsEpilog(this.XmlContentDocument);
			XmlElement xmlElement = SpiritResolver.WorksheetRootNode(this.XmlContentDocument, this.NameSpaceManager, this._worksheetNodeName) as XmlElement;
			if (xmlElement != null)
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

		private readonly Action<XmlSchemaException> _onWriteValidationFailed;

		private readonly ISerializationStrategy _serializationStrategy;

		private readonly ISerializationHelper _serializationHelper;

		private readonly IRegionCollectionSerializer _regionCollectionSerializer;

		private readonly PackagePart _packagePart;

		private readonly string _worksheetNodeName;
	}
}
