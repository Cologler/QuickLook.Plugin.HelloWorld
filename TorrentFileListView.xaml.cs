﻿// Copyright © 2017 Paddy Xu
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.TorrentViewer
{
    /// <summary>
    ///     Interaction logic for ArchiveFileListView.xaml
    /// </summary>
    public partial class TorrentFileListView : UserControl, IDisposable
    {
        public TorrentFileListView()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            IconManager.ClearCache();
        }

        public void SetDataContext(object context)
        {
            treeGrid.DataContext = context;

            treeView.LayoutUpdated += (sender, e) =>
            {
                // return when empty
                if (treeView.Items.Count == 0)
                    return;

                // return when there are more than one root nodes
                if (treeView.Items.Count > 1)
                    return;

                var root = (TreeViewItem) treeView.ItemContainerGenerator.ContainerFromItem(treeView.Items[0]);
                if (root == null)
                    return;

                root.IsExpanded = true;
            };
        }

        private void CopyMd5SumLower_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TorrentFileEntry fileEntry && fileEntry.Md5Sum?.Length > 0)
            {
                try
                {
                    Clipboard.SetText(fileEntry.Md5Sum.ToLower());
                    return;
                }
                catch (Exception) { }

                MessageBox.Show("Failed to copy Md5Sum.");
            }
        }

        private void CopyMd5SumUpper_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TorrentFileEntry fileEntry && fileEntry.Md5Sum?.Length > 0)
            {
                try
                {
                    Clipboard.SetText(fileEntry.Md5Sum.ToUpper());
                    return;
                }
                catch (Exception) { }

                MessageBox.Show("Failed to copy Md5Sum.");
            }
        }
    }
}