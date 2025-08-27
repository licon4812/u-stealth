﻿using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    /// <summary>
    /// ViewModel for the options flyout in the TableViewColumnHeader.
    /// </summary>
    private partial class OptionsFlyoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private IList<TableViewFilterItem> _filterItems = [];
        private bool _canSetState = true;

        /// <summary>
        /// Initializes a new instance of the OptionsFlyoutViewModel class.
        /// </summary>
        /// <param name="tableView">The TableView associated with the ViewModel.</param>
        /// <param name="columnHeader">The TableViewColumnHeader associated with the ViewModel.</param>
        public OptionsFlyoutViewModel(TableView tableView, TableViewColumnHeader columnHeader)
        {
            TableView = tableView;
            ColumnHeader = columnHeader;
            InitializeCommands();
        }

        /// <summary>
        /// Initializes the commands for the ViewModel.
        /// </summary>
        private void InitializeCommands()
        {
            SortAscendingCommand.ExecuteRequested += delegate { ColumnHeader.DoSort(SD.Ascending); };
            SortAscendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.CanSort && ColumnHeader.Column?.SortDirection != SD.Ascending;

            SortDescendingCommand.ExecuteRequested += delegate { ColumnHeader.DoSort(SD.Descending); };
            SortDescendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.CanSort && ColumnHeader.Column?.SortDirection != SD.Descending;

            ClearSortingCommand.ExecuteRequested += delegate { ColumnHeader.ClearSortingWithEvent(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.Column?.SortDirection is not null;

            ClearFilterCommand.ExecuteRequested += delegate { ColumnHeader.ClearFilter(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.Column?.IsFiltered is true;

            OkCommand.ExecuteRequested += delegate
            {
                ColumnHeader.HideFlyout();
                ColumnHeader.ApplyFilter();
            };

            CancelCommand.ExecuteRequested += delegate { ColumnHeader.HideFlyout(); };
        }

        /// <summary>
        /// Attaches property changed handlers to the filter items.
        /// </summary>
        private void AttachPropertyChangedHandlers()
        {
            if (_filterItems?.Count > 0)
            {
                foreach (var item in _filterItems)
                {
                    item.PropertyChanged += OnFilterItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Detaches property changed handlers from the filter items.
        /// </summary>
        private void DetachPropertyChangedHandlers()
        {
            if (_filterItems?.Count > 0)
            {
                foreach (var item in _filterItems)
                {
                    item.PropertyChanged -= OnFilterItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Handles the PropertyChanged event for filter items.
        /// </summary>
        private void OnFilterItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SetSelectAllCheckBoxState();
        }

        /// <summary>
        /// Gets the TableView associated with the ViewModel.
        /// </summary>
        public TableView TableView { get; }

        /// <summary>
        /// Gets the TableViewColumnHeader associated with the ViewModel.
        /// </summary>
        public TableViewColumnHeader ColumnHeader { get; }

        /// <summary>
        /// Gets or sets the filter items.
        /// </summary>
        public IList<TableViewFilterItem> FilterItems
        {
            get => _filterItems;
            set
            {
                if (_filterItems == value) return;

                DetachPropertyChangedHandlers();
                _filterItems = value;
                AttachPropertyChangedHandlers();
                SetSelectAllCheckBoxState();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the selected values for the filter.
        /// </summary>
        public List<object> SelectedValues { get; set; } = [];

        /// <summary>
        /// Sets the state of the select all checkbox.
        /// </summary>
        private void SetSelectAllCheckBoxState()
        {
            if (ColumnHeader._selectAllCheckBox is null || !_canSetState)
            {
                return;
            }

            ColumnHeader._selectAllCheckBox.IsChecked = _filterItems.All(x => x.IsSelected)
                                                        ? true
                                                        : _filterItems.All(x => !x.IsSelected)
                                                        ? false
                                                        : null;
        }

        /// <summary>
        /// Sets the state of the filter items.
        /// </summary>
        /// <param name="isSelected">The state to set.</param>
        internal void SetFilterItemsState(bool isSelected)
        {
            _canSetState = false;

            foreach (var item in FilterItems)
            {
                item.IsSelected = isSelected;
            }

            _canSetState = true;
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = default)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets the command to sort in ascending order.
        /// </summary>
        public StandardUICommand SortAscendingCommand { get; } = new() { Label = TableViewLocalizedStrings.SortAscending };

        /// <summary>
        /// Gets the command to sort in descending order.
        /// </summary>
        public StandardUICommand SortDescendingCommand { get; } = new() { Label = TableViewLocalizedStrings.SortDescending };

        /// <summary>
        /// Gets the command to clear sorting.
        /// </summary>
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = TableViewLocalizedStrings.ClearSorting };

        /// <summary>
        /// Gets the command to clear the filter.
        /// </summary>
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = TableViewLocalizedStrings.ClearFilter };

        /// <summary>
        /// Gets the command to confirm the filter.
        /// </summary>
        public StandardUICommand OkCommand { get; } = new() { Label = TableViewLocalizedStrings.Ok };

        /// <summary>
        /// Gets the command to cancel the filter.
        /// </summary>
        public StandardUICommand CancelCommand { get; } = new() { Label = TableViewLocalizedStrings.Cancel };
    }
}