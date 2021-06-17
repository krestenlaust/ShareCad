﻿using Ptc;
using Ptc.Controls.Core.Serialization;
using Ptc.Controls.Worksheet;
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;
using System.Collections.Generic;
using System.Windows;
using System.Xml;

namespace ShareCad
{
    public class CustomWorksheetSectionSerializationStrategy : ISerializationStrategy
	{
		public readonly IWorksheetSectionPersistentData _worksheetSectionData;

		public CustomWorksheetSectionSerializationStrategy(IWorksheetSectionPersistentData sectionData, DelegateFunction0<IRegionType> worksheetSectionRegionDataCreator)
		{
			_worksheetSectionData = sectionData;
			_worksheetSectionRegionDataCreator = worksheetSectionRegionDataCreator;
		}
		
		public readonly DelegateFunction0<IRegionType> _worksheetSectionRegionDataCreator;

		public void SerializeRegionsEpilog(XmlDocument xmlDocument)
		{
		}

		public IList<IRegionType> GetRegions(IS11NProvider s11NProvider)
		{
			List<IRegionType> list = new List<IRegionType>();

			foreach (KeyValuePair<UIElement, Point> keyValuePair in _worksheetSectionData.RegionsToSerialize)
			{
				IRegionInWhiteboardType regionInWhiteboardType = RegionFactory.Instance.ConvertToIRegionType(_worksheetSectionRegionDataCreator, s11NProvider, keyValuePair.Key) as IRegionInWhiteboardType;
				regionInWhiteboardType.Location = keyValuePair.Value;
				list.Add(regionInWhiteboardType);
			}

			return list;
		}
	}
}
