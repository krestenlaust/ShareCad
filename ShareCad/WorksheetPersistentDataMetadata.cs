using HarmonyLib;
using Microsoft.Win32.SafeHandles;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using Ptc.Wpf;
using Ptc.PersistentData;
using Spirit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using Ptc.Serialization;
using Ptc.PersistentDataObjects;
using System.Runtime.Serialization;
using Ptc.Core;

namespace ShareCad
{
    [DataContract]
    //[MetadataType(typeof(WorksheetPersistentDataMetadata))]
    public class SerializableWorksheetPersistentData
    {
        public string LayoutSize { get; set; }
        public string GridSize { get; set; }
        public string PageOrientation { get; set; }
        public bool DisplayGrid { get; set; }
        public bool DisplayHFGrid { get; set; }

        [DataMember]
        public BackgroundTypeE PlotBackgroundType { get; set; }
        [DataMember]
        public SerializableWorksheetSectionPersistentData Header { get; }
        [DataMember]
        public SerializableWorksheetSectionPersistentData WorksheetContent { get; }
        [DataMember]
        public SerializableWorksheetSectionPersistentData Footer { get; }

        public bool ShowIOTags { get; set; }
        public bool OleObjectAutoResize { get; set; }
        
        public SerializableWorksheetPersistentData()
        {

        }

        public SerializableWorksheetPersistentData(IWorksheetPersistentData data)
        {
            LayoutSize = data.LayoutSize;
            GridSize = data.GridSize;
            PageOrientation = data.PageOrientation;
            DisplayGrid = data.DisplayGrid;
            DisplayHFGrid = data.DisplayHFGrid;

            PlotBackgroundType = data.PlotBackgroundType;
            Header = new SerializableWorksheetSectionPersistentData(data.Header);
            WorksheetContent = new SerializableWorksheetSectionPersistentData(data.WorksheetContent);
            Footer = new SerializableWorksheetSectionPersistentData(data.Footer);

            ShowIOTags = data.ShowIOTags;
            OleObjectAutoResize = data.OleObjectAutoResize;
        }
    }

    [DataContract]
    public class SerializableWorksheetSectionPersistentData
    {
        [DataMember]
        public List<SerializableRegionPersistentData> SerializedRegions { get; }
        [DataMember]
        public Dictionary<UIElement, Point> RegionsToSerialize { get; }
        [DataMember]
        public bool ConvertedFromMC14 { get; set; }

        public SerializableWorksheetSectionPersistentData(IWorksheetSectionPersistentData data)
        {
            SerializedRegions = new List<SerializableRegionPersistentData>(data.SerializedRegions.Count);
            foreach (var item in data.SerializedRegions)
            {
                SerializedRegions.Add(new SerializableRegionPersistentData(item));
            }

            RegionsToSerialize = new Dictionary<UIElement, Point>(data.RegionsToSerialize);

            ConvertedFromMC14 = data.ConvertedFromMC14;
        }
    }

    [DataContract]
    public class SerializableRegionPersistentData
    {
        [DataMember]
        public UIElement Control { get; }
        [DataMember]
        public Point Location { get; set; }
        [DataMember]
        public bool HasDeserializationErrors { get; }
        [DataMember]
        public SerializableRegionType RegionData { get; }

        public SerializableRegionPersistentData(IRegionPersistentData data)
        {
            Control = data.Control;
            Location = data.Location;
            HasDeserializationErrors = data.HasDeserializationErrors;
            RegionData = new SerializableRegionType(data.RegionData);
        }
    }

    [DataContract]
    public class SerializableRegionType
    {
        public string regionid { get; set; }
        public double height { get; }
        public bool heightSpecified { get; }
        public double width { get; }
        public bool widthSpecified { get; }
        public double actualWidth { get; set; }
        public bool actualWidthSpecified { get; set; }
        public double actualHeight { get; set; }
        public bool actualHeightSpecified { get; set; }
        public string[] mc14conversionlossannotations { get; set; }
        public object Item { get; set; }
        public bool mc14conversionloss { get; }
        public bool ConvertedFromMC14 { get; }

        public SerializableRegionType(IRegionType data)
        {
            regionid = data.regionid;
            height = data.height;
            heightSpecified = data.heightSpecified;
            width = data.width;
            widthSpecified = data.widthSpecified;
            actualWidth = data.actualWidth;
            actualWidthSpecified = data.actualWidthSpecified;
            actualHeight = data.actualHeight;
            actualHeightSpecified = data.actualHeightSpecified;
            mc14conversionlossannotations = data.mc14conversionlossannotations;
            Item = data.Item;
            mc14conversionloss = data.mc14conversionloss;
            ConvertedFromMC14 = data.ConvertedFromMC14;
        }
    }

    [DataContract]
    class WorksheetPersistentDataMetadata
    {
        public string LayoutSize { get; set; }
        public string GridSize { get; set; }
        public string PageOrientation { get; set; }
        public bool DisplayGrid { get; set; }
        public bool DisplayHFGrid { get; set; }

        public BackgroundTypeE PlotBackgroundType { get; set; }
        public IWorksheetSectionPersistentData Header { get; }
        public IWorksheetSectionPersistentData WorksheetContent { get; }
        public IWorksheetSectionPersistentData Footer { get; }

        public bool ShowIOTags { get; set; }
        public bool OleObjectAutoResize { get; set; }
    }
}
