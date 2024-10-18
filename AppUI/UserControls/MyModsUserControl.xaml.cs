using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using AppUI.Windows;
using AppUI;
using AppUI.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for MyModsUserControl.xaml
    /// </summary>
    public partial class MyModsUserControl : UserControl
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MyModsViewModel ViewModel { get; set; }
        public bool IsDragging { get; private set; }

        Dictionary<string, double> MinColumnWidths = new Dictionary<string, double>()
        {
            { "Name", 100 },
            { "Author", 60 },
            { "Category", 140 },
            { "Active", 80 },
            { "Miscellaneous", 300 },
        };

        Dictionary<string, string> ColumnTranslations
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { ResourceHelper.Get(StringKey.Name), "Name" },
                    { ResourceHelper.Get(StringKey.Author), "Author" },
                    { ResourceHelper.Get(StringKey.Category), "Category" },
                    { ResourceHelper.Get(StringKey.Active), "Active" },
                    { ResourceHelper.Get(StringKey.Miscellaneous), "Miscellaneous" },
                };
            }
        }

        public MyModsUserControl()
        {
            InitializeComponent();
        }

        public void SetDataContext(MyModsViewModel viewModel)
        {
            ViewModel = viewModel;
            this.DataContext = ViewModel;
        }

        /// <summary>
        /// Returns true if a mod is selected in the list.
        /// Returns false and shows messagebox warning user that no mod is selected otherwise;
        /// </summary>
        private bool IsModSelected()
        {
            if (lstMods.SelectedItem == null)
            {
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.SelectAModFirst), true));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the selected mod is active.
        /// Returns false and shows messagebox warning user that selected mod is not active otherwise;
        /// </summary>
        private bool IsActiveModSelected(string notActiveMessage)
        {
            if (lstMods.SelectedItem == null)
            {
                return false;
            }

            if (!(lstMods.SelectedItem as InstalledModViewModel).IsActive)
            {
                Sys.Message(new WMessage(notActiveMessage, true));
                return false;
            }

            return true;
        }

        private void lstMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstMods.SelectedItem == null)
            {
                return;
            }

            ViewModel.RaiseSelectedModChanged(sender, (lstMods.SelectedItem as InstalledModViewModel));
        }

        private void btnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Sys.ValidateAndRemoveDeletedMods();
            Task t = ViewModel.RefreshModListAsync();

            t.ContinueWith((result) =>
            {
                App.Current.Dispatcher.Invoke(() => RecalculateColumnWidths());
            });
        }

        private void btnDeactivateAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DeactivateAllActiveMods();
        }

        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (!IsModSelected())
            {
                return;
            }

            InstalledModViewModel selected = (lstMods.SelectedItem as InstalledModViewModel);

            MessageDialogWindow messageDialog = new MessageDialogWindow(ResourceHelper.Get(StringKey.UninstallWarning), $"{ResourceHelper.Get(StringKey.AreYouSureYouWantToDelete)} {selected.Name}?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            messageDialog.ShowDialog();

            if (messageDialog.ViewModel.Result == MessageBoxResult.Yes)
            {
                ViewModel.UninstallMod(selected);
            }

        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (!IsModSelected())
            {
                return;
            }

            ViewModel.ReorderProfileItem((lstMods.SelectedItem as InstalledModViewModel), -1);
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (!IsModSelected())
            {
                return;
            }

            ViewModel.ReorderProfileItem((lstMods.SelectedItem as InstalledModViewModel), 1);
        }

        private void btnMoveTop_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsModSelected())
            {
                return;
            }

            ViewModel.SendModToTop((lstMods.SelectedItem as InstalledModViewModel));
        }

        private void btnSendBottom_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsModSelected())
            {
                return;
            }

            ViewModel.SendModToBottom((lstMods.SelectedItem as InstalledModViewModel));
        }

        private void btnActivateAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ActivateAllMods();
        }

        private void btnConfigure_Click(object sender, RoutedEventArgs e)
        {
            var src = (Button)e.OriginalSource;

            // If we arrive here through the "Configure Mod" button in the list item, then select the current item in list and proceed as usual
            if (src.DataContext.GetType() == typeof(InstalledModViewModel))
            {
                lstMods.SelectedItem = src.DataContext;
            }

            if (!IsModSelected())
            {
                return;
            }

            if (!IsActiveModSelected(ResourceHelper.Get(StringKey.ModNotActiveOnlyActivatedModsCanBeConfigured)))
            {
                return;
            }

            ViewModel.ShowConfigureModWindow((lstMods.SelectedItem as InstalledModViewModel));
        }

        private void btnUpdateMod_Click(object sender, RoutedEventArgs e)
        {
            var src = (Button)e.OriginalSource;
            var mod = (InstalledModViewModel)src.DataContext;

            if (mod.InstallInfo.IsUpdateAvailable)
            {
                lstMods.SelectedItem = src.DataContext;
                src.Content = ResourceHelper.Get(StringKey.UpdateDownloading);

                Install.DownloadAndInstall(Sys.GetModFromCatalog(mod.InstallInfo.ModID), true);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowImportModWindow();
        }

        private void btnAutoSort_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoSortBasedOnCategory();
        }

        /// <summary>
        /// Opens the configure mod window for selected mod (if mod is active)
        /// </summary>
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as ListViewItem) != null)
            {
                InstalledModViewModel viewModel = (sender as ListViewItem).DataContext as InstalledModViewModel;

                if (viewModel.IsActive)
                {
                    ViewModel.ShowConfigureModWindow(viewModel);
                }
                else
                {
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.ModNotActiveOnlyActivatedModsCanBeConfigured), true));
                }
            }
        }

        /// <summary>
        /// Activates the selected mod on middle click
        /// </summary>
        private void ListViewItem_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle && e.ButtonState == System.Windows.Input.MouseButtonState.Released)
            {
                if ((sender as ListViewItem) != null)
                {
                    InstalledModViewModel viewModel = (sender as ListViewItem).DataContext as InstalledModViewModel;
                    ViewModel.ToggleActivateMod(viewModel.InstallInfo.ModID);
                }
            }
        }

        internal void RecalculateColumnWidths(double listWidth)
        {
            var currentColumnSettings = GetColumnSettings();

            double staticColumnWidth = currentColumnSettings.Where(c => !c.AutoResize).Sum(s => s.Width); // sum of columns with static widths
            double padding = 8;

            if (listWidth == 0)
            {
                return; // ActualWidth could be zero if list has not been rendered yet
            }


            // account for the scroll bar being visible and add extra padding
            ScrollViewer sv = FindVisualChild<ScrollViewer>(lstMods);
            Visibility? scrollVis = sv?.ComputedVerticalScrollBarVisibility;

            if (scrollVis.GetValueOrDefault(Visibility.Collapsed) == Visibility.Visible)
            {
                padding = 26;
            }


            double remainingWidth = listWidth - staticColumnWidth - padding;

            double nameWidth = (0.66) * remainingWidth; // Name takes 66% of remaining width
            double authorWidth = (0.33) * remainingWidth; // Author takes up 33% of remaining width

            double minNameWidth = 100; // don't resize columns less than the minimums
            double minAuthorWidth = 60;

            try
            {
                if (nameWidth < listWidth && nameWidth > minNameWidth)
                {
                    colName.Width = nameWidth;
                }

                if (authorWidth < listWidth && authorWidth > minAuthorWidth)
                {
                    colAuthor.Width = authorWidth;
                }
            }
            catch (System.Exception e)
            {
                Logger.Warn(e, "failed to resize columns");
            }
        }

        internal void RecalculateColumnWidths()
        {
            RecalculateColumnWidths(lstMods.ActualWidth);
        }

        internal List<ColumnInfo> GetColumnSettings()
        {
            GridViewColumnCollection columns = (lstMods.View as GridView).Columns;

            if (columns == null)
            {
                return null;
            }

            bool hasInvalidColumns = columns.Any(c =>
            {
                ColumnTranslations.TryGetValue((c.Header as GridViewColumnHeader).Content as string, out string translatedName);
                return string.IsNullOrEmpty(translatedName);
            });

            if (hasInvalidColumns)
            {
                return null; // exit out if can not translate columns correctly (happens if not initialized in Visual Tree yet and switch languages)
            }

            List<string> columnsThatAutoResize = new List<string>() { "Name", "Author" };

            return columns.Select(c => new ColumnInfo()
            {
                Name = ColumnTranslations[(c.Header as GridViewColumnHeader).Content as string],
                Width = c.ActualWidth,
                AutoResize = columnsThatAutoResize.Contains(ColumnTranslations[((c.Header as GridViewColumnHeader).Content as string)])
            }).ToList();
        }

        /// <summary>
        /// Initiates the Drag/Drop re-ordering of active mods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstMods_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (e.OriginalSource is CheckBox || FindVisualParent<ComboBox>((DependencyObject)e.OriginalSource) != null || e.OriginalSource is Button)
                {
                    return; // do not do drag/drop since user is clicking on checkbox to activate mod or selecting category or clicking one of the misc buttons
                }

                if ((e.OriginalSource as FrameworkElement)?.DataContext is InstalledModViewModel)
                {
                    ListViewItem lbi = GetListViewItemFromObject(e.OriginalSource);

                    if (lbi != null)
                    {
                        IsDragging = true;
                        DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
                    }
                }
            }
        }

        /// <summary>
        /// Re-orders active mods after Drag/Drop has been "dropped"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstMods_Drop(object sender, DragEventArgs e)
        {
            try
            {
                ListViewItem targetItem = GetListViewItemFromObject(e.OriginalSource);

                if (targetItem == null)
                {
                    return;
                }

                Point mousePos = e.GetPosition(targetItem);
                double halfHeight = targetItem.ActualHeight / 2.0;

                InstalledModViewModel target = targetItem.DataContext as InstalledModViewModel;
                InstalledModViewModel droppedData = e.Data.GetData(typeof(InstalledModViewModel)) as InstalledModViewModel;

                int removedIdx = lstMods.Items.IndexOf(droppedData);
                int targetIdx = lstMods.Items.IndexOf(target);

                if (mousePos.Y >= halfHeight && removedIdx > targetIdx && (targetIdx + 1) < lstMods.Items.Count)
                {
                    targetIdx++;
                }
                else if (mousePos.Y < halfHeight && removedIdx < targetIdx && (targetIdx - 1) >= 0)
                {
                    targetIdx--;
                }

                if (targetIdx == removedIdx)
                {
                    return; // no movement needed
                }

                int delta = targetIdx - removedIdx;

                ViewModel.ReorderProfileItem(droppedData, delta);
            }
            catch (System.Exception ex)
            {
                Logger.Warn("Failed to drag/drop mods");
                Logger.Error(ex);
            }
            finally
            {
                IsDragging = false;
            }
        }

        private void ListViewItem_PreviewDragEnter(object sender, DragEventArgs e)
        {
            UpdateDragDropIndicatorLine(sender, e);
        }

        private void ListViewItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            UpdateDragDropIndicatorLine(sender, e);
        }

        /// <summary>
        /// Updates the border thickness of a ListViewItem during the drag and drop
        /// to show visual indicator of where the dragged item will be dropped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDragDropIndicatorLine(object sender, DragEventArgs e)
        {
            ListViewItem item = GetListViewItemFromObject(sender);

            if (item == null)
            {
                return;
            }

            Point mousePos = e.GetPosition(item);
            double halfHeight = item.ActualHeight / 2.0;


            InstalledModViewModel dataToDrop = e.Data.GetData(typeof(InstalledModViewModel)) as InstalledModViewModel;
            InstalledModViewModel hoverOver = (item.DataContext as InstalledModViewModel);

            // check if dragging over self - don't show border in this case
            int removedIdx = lstMods.Items.IndexOf(dataToDrop);
            int targetIdx = lstMods.Items.IndexOf(hoverOver);

            if (mousePos.Y >= halfHeight && removedIdx > targetIdx && (targetIdx + 1) < lstMods.Items.Count)
            {
                targetIdx++;
            }
            else if (mousePos.Y < halfHeight && removedIdx < targetIdx && (targetIdx - 1) >= 0)
            {
                targetIdx--;
            }

            if (dataToDrop == hoverOver || targetIdx == removedIdx)
            {
                hoverOver.BorderThickness = new Thickness(0);
                return;
            }


            if (mousePos.Y < halfHeight)
            {
                hoverOver.BorderThickness = new Thickness(0, 2, 0, 0);
            }
            else
            {
                hoverOver.BorderThickness = new Thickness(0, 0, 0, 2);
            }
        }

        private void ListViewItem_PreviewDragLeave(object sender, DragEventArgs e)
        {
            ListViewItem item = GetListViewItemFromObject(sender);

            if (item == null)
            {
                return;
            }

            (item.DataContext as InstalledModViewModel).BorderThickness = new Thickness(0);
        }

        private ListViewItem GetListViewItemFromObject(object sender)
        {
            ListViewItem item = sender as ListViewItem;

            if (item == null)
            {
                item = FindVisualParent<ListViewItem>((DependencyObject)sender);
            }

            return item;
        }

        private T FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Ensure column is not resized to less than the defined minimum width.
        /// Sets width to minimum width if less than minimum.
        /// </summary>
        private void GridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            string columnName = (e.OriginalSource as GridViewColumnHeader).Content as string;

            if (e.NewSize.Width < MinColumnWidths[ColumnTranslations[columnName]])
            {
                e.Handled = true;
                ((GridViewColumnHeader)sender).Column.Width = MinColumnWidths[ColumnTranslations[columnName]];
            }
        }

        private void btnResetColumns_Click(object sender, RoutedEventArgs e)
        {
            List<ColumnInfo> defaultColumns = ColumnSettings.GetDefaultSettings().MyModsColumns;
            ApplyColumnSettings(defaultColumns);
        }

        internal void ApplyColumnSettings(List<ColumnInfo> newColumns)
        {
            GridViewColumnCollection columns = (lstMods.View as GridView).Columns;

            for (int i = 0; i < newColumns.Count; i++)
            {
                // get the current index of the column
                int colIndex = columns.ToList().FindIndex(c => ColumnTranslations[((c.Header as GridViewColumnHeader).Content as string)] == newColumns[i].Name);

                // apply the new width if the column does not auto resize
                if (!newColumns[i].AutoResize)
                {
                    columns[colIndex].Width = newColumns[i].Width;
                }

                // move the column to expected index
                columns.Move(colIndex, i);
            }

            RecalculateColumnWidths(); // call this to have auto resize columns recalculate
        }
    }
}
