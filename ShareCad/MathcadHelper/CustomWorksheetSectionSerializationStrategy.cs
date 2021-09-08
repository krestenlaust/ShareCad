using System.Collections.Generic;
using System.Windows;
using System.Xml;
using Ptc;
using Ptc.Controls.Core.Serialization;
using Ptc.Controls.Worksheet;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;

namespace ShareCad
{
    public class CustomWorksheetSectionSerializationStrategy : ISerializationStrategy
	{
		private readonly IDictionary<UIElement, Point> _regionData;

		public CustomWorksheetSectionSerializationStrategy(IDictionary<UIElement, Point> serializableRegions, DelegateFunction0<IRegionType> worksheetSectionRegionDataCreator)
		{
			_regionData = serializableRegions;
			_worksheetSectionRegionDataCreator = worksheetSectionRegionDataCreator;
		}
		
		public readonly DelegateFunction0<IRegionType> _worksheetSectionRegionDataCreator;

		public void SerializeRegionsEpilog(XmlDocument xmlDocument)
		{
		}

		public IList<IRegionType> GetRegions(IS11NProvider s11NProvider)
		{
			List<IRegionType> list = new List<IRegionType>();

			foreach (KeyValuePair<UIElement, Point> keyValuePair in _regionData)
			{
				// preserve region ID
				long previousRegionID = s11NProvider.Helper.GetRegionId(keyValuePair.Key);
				IRegionInWhiteboardType regionInWhiteboardType = RegionFactory.Instance.ConvertToIRegionType(_worksheetSectionRegionDataCreator, s11NProvider, keyValuePair.Key) as IRegionInWhiteboardType;
				s11NProvider.Helper.SetRegionId(keyValuePair.Key, previousRegionID);
				regionInWhiteboardType.regionid = previousRegionID.ToString();

				regionInWhiteboardType.Location = keyValuePair.Value;
				list.Add(regionInWhiteboardType);
			}

			return list;
		}
	}
}
