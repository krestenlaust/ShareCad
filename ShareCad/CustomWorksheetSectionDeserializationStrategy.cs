using Ptc.Controls.Core;
using Ptc.PersistentData;
using System;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using Ptc.Serialization;
using System.Collections.Generic;
using Ptc.Controls.Core.Serialization;
using Ptc.Controls.Include;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    public class CustomWorksheetSectionDeserializationStrategy : DeserializationStrategyWithMathFormat
    {
        public CustomWorksheetSectionDeserializationStrategy(IWorksheetSectionPersistentData sectionData, IMathFormat mathFormat, IDictionary<IdLabels, ExtendedFont> labeledIdFormat)
        {
            this._worksheetSectionData = sectionData;
            base.DeserializeMathFormat = mathFormat;
            base.DeserializeLabeledIdFormat = labeledIdFormat;
        }

        public override void DeserializeRegionProlog(UIElement region)
        {
            IMultiLevelInclusionProvider multiLevelInclusionProvider = region as IMultiLevelInclusionProvider;
            if (multiLevelInclusionProvider != null)
            {
                multiLevelInclusionProvider.MultiLevelInclusionItem.CacheValid = true;
            }
            base.DeserializeRegionProlog(region);
        }

        public override IList<UIElement> Regions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void AddRegion(UIElement element, Point location)
        {
            throw new NotImplementedException();
        }

        public override void DeleteRegion(UIElement control)
        {
            throw new NotImplementedException();
        }

        public override IRegionFactory RegionFactory
        {
            get
            {
                return Ptc.Controls.Worksheet.RegionFactory.Instance;
            }
        }

        protected override void AddRegion(IRegionPersistentData regionPersistentData)
        {
            this._worksheetSectionData.SerializedRegions.Add(regionPersistentData);
        }

        protected override void CleanUpLastRegion(IRegionPersistentData regionPersistentData)
        {
            this._worksheetSectionData.SerializedRegions.Remove(regionPersistentData);
        }

        private readonly IWorksheetSectionPersistentData _worksheetSectionData;
    }
}
