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
using Ptc.Controls.Whiteboard;

namespace ShareCad
{
    public static class WorksheetControlExtensions
    {
        public static IWorksheetViewModel GetViewModel(this WorksheetControl control) => control.DataContext as IWorksheetViewModel;

        public static Point GridLocationToWorksheetLocation(this IWorksheetViewModel viewModel, Point gridLocation, bool useContentGrid=true)
        {
            WorksheetPageLayout pageLayout = viewModel.PageLayout;
            Size size = useContentGrid ? pageLayout.GridCellSize : pageLayout.HeaderFooterGridCellSize;
            return new Point(gridLocation.X * size.Width, gridLocation.Y * size.Height);
        }

        public static Point WorksheetLocationToGridLocation(this IWorksheetViewModel viewModel, Point worksheetLocation, bool useContentGrid=true)
        {
            WorksheetPageLayout pageLayout = viewModel.PageLayout;
            Size size = useContentGrid ? pageLayout.GridCellSize : pageLayout.HeaderFooterGridCellSize;
            return new Point(Math.Round(worksheetLocation.X / size.Width), Math.Round(worksheetLocation.Y / size.Height));
        }
    }
}
