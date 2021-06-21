using Ptc;
using Ptc.Calculation.Core;
using Ptc.Controls.Core;
using Ptc.Controls.Core.Serialization;
using Ptc.Controls.ExcelComponent;
using Ptc.Controls.Include;
using Ptc.Controls.Whiteboard;
using Ptc.Controls.Worksheet;
using Ptc.PersistentData;
using Ptc.Serialization;
using Ptc.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

		public static void ApplyWorksheetDataLite(this WorksheetControl worksheetControl, IWorksheetPersistentData worksheetData)
		{
			var _viewModel = worksheetControl.GetViewModel();

			_viewModel.DisplayGrid = worksheetData.DisplayGrid;
			_viewModel.DisplayHFGrid = worksheetData.DisplayHFGrid;
			worksheetControl.PlotBackgroundType = worksheetData.PlotBackgroundType;
			worksheetControl.ShowInputOutputDesignation = worksheetData.ShowIOTags;
			_viewModel.OleObjectAutoResize = worksheetData.OleObjectAutoResize;
			IList<IRegionPersistentData> contentRegions = worksheetData.WorksheetContent.SerializedRegions;

			if (!contentRegions.IsNullOrEmpty())
			{
				//_viewModel.InsertionPoint = new Point(0.0, 0.0);
				contentRegions.Each(delegate (IRegionPersistentData r)
				{
					IWorksheetPageBreak worksheetPageBreak = r.Control as IWorksheetPageBreak;
					if (worksheetPageBreak != null)
					{
						_viewModel.WorksheetContent.AddPageBreakAtLocation(worksheetPageBreak, r.Location, true);
					}
				});
				bool assertOnGridPosition = !worksheetData.WorksheetContent.ConvertedFromMC14;
				//IWorksheetItemManager itemManager = WorksheetViewModel.ItemManager;
				contentRegions.Each(delegate (IRegionPersistentData r)
				{
					IWhiteboardItem whiteboardItem = r.Control as IWhiteboardItem;
					if (whiteboardItem != null)
					{
						IWhiteboardControl whiteboardControl = whiteboardItem as IWhiteboardControl;
						if (whiteboardControl != null)
						{
							whiteboardControl.GridCellSize = worksheetControl.GridCellSize;
						}
						IExcelComponentControl excelComponentControl = whiteboardItem as IExcelComponentControl;
						if (excelComponentControl != null)
						{
							excelComponentControl.UpdateSizeLimits(Size.Empty);
						}
						IPageWideSizable pageWideSizable = whiteboardItem as IPageWideSizable;
						if (pageWideSizable != null && !(pageWideSizable is IIncludeWorksheetControl))
						{
							pageWideSizable.NotHookupSizeChangedHandlerUntilLoaded = true;
						}
						//itemManager.SetShouldAssertOnLocation(whiteboardItem, assertOnGridPosition);
						_viewModel.WorksheetContent.AddItemAtLocation(whiteboardItem, r.Location, true);
						//itemManager.ClearShouldAssertOnLocation(whiteboardItem);
						_viewModel.WorksheetContent.CommitItem(whiteboardItem);
					}
				});
				contentRegions.Each(delegate (IRegionPersistentData regionPersistentData)
				{
					if (SerializationHelperUtils.IsConvertedFromMathcad14(regionPersistentData.Control))
					{
						SetStatusSynchronized(regionPersistentData.Control);
					}
				});
				Action action = delegate ()
				{
					(from item in contentRegions
					 where item.Control is IPageWideSizable && !(item.Control is IIncludeWorksheetControl)
					 select item).Each(delegate (IRegionPersistentData item)
					 {
						 IPageWideSizable pageWideSizable = item.Control as IPageWideSizable;
						 pageWideSizable.FireHookupSizeChangedHandler();
					 });
					if (worksheetData.WorksheetContent.ConvertedFromMC14)
					{
						//itemManager.ExecuteOnLayoutUpdated(worksheetControl, delegate
						//{
							//WhiteboardCommands.SeparateOverlappedItemsVertically.Execute(null, worksheetControl);
							//CommandBuffer.RemoveTopUndo();
						//});
					}
					if (_viewModel.IsValidInsertionPoint(_viewModel.PageLayout.InitialInsertionPoint))
					{
						_viewModel.InsertionPoint = _viewModel.PageLayout.InitialInsertionPoint;
					}
				};
				//itemManager.ExecuteInBackground(action, false);
			}
			worksheetData.Header.SerializedRegions.Each(delegate (IRegionPersistentData r)
			{
				_viewModel.Header.AddItemAtLocation(r.Control as IWhiteboardItem, r.Location, false);
			});
			worksheetData.Footer.SerializedRegions.Each(delegate (IRegionPersistentData r)
			{
				_viewModel.Footer.AddItemAtLocation(r.Control as IWhiteboardItem, r.Location, false);
			});

			_viewModel.PopulateZombies();
		}

		private static void SetStatusSynchronized(object control)
		{
			ICalculatable calculatable = control as ICalculatable;
			if (calculatable != null)
			{
				calculatable.SetStatusSynchronized();
			}
			ICalculatableContainer calculatableContainer = control as ICalculatableContainer;
			if (calculatableContainer != null)
			{
				foreach (ICalculatable statusSynchronized in calculatableContainer.CalculatableChildren)
				{
					SetStatusSynchronized(statusSynchronized);
				}
			}
		}
	}
}
