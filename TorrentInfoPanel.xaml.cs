// Copyright © 2017 Paddy Xu
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using BencodeNET.IO;
using BencodeNET.Torrents;

using QuickLook.Common.Annotations;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.TorrentViewer
{
    /// <summary>
    ///     Interaction logic for ArchiveInfoPanel.xaml
    /// </summary>
    public partial class TorrentInfoPanel : UserControl, IDisposable, INotifyPropertyChanged
    {
        private bool _disposed;
        private double _loadPercent;
        private string _magnetUri;

        public TorrentInfoPanel(string path)
        {
            InitializeComponent();

            // design-time only
            Resources.MergedDictionaries.Clear();

            BeginLoadArchive(path);
        }

        public double LoadPercent
        {
            get => _loadPercent;
            private set
            {
                if (value == _loadPercent)
                    return;
                _loadPercent = value;
                OnPropertyChanged();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _disposed = true;

            fileListView.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BeginLoadArchive(string path)
        {
            Task.Run(() =>
            {
                Dictionary<string, TorrentFileEntry> nodeEntires;
                try
                {
                    nodeEntires = LoadFromTorrent(path);
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    Dispatcher.Invoke(() => lblLoading.Content = $"Preview failed:\n{e.Message}");
                    return;
                }

                var root = nodeEntires[string.Empty];

                var files = nodeEntires
                    .Where(z => !z.Value.IsFolder)
                    .Select(z => z.Value)
                    .ToList();

                var paddingFilesCount = files.Count(z => z.IsPaddingFile);
                var totalFilesCount = $"Total {files.Count} files";
                if (paddingFilesCount > 0)
                {
                    totalFilesCount += $" (with hided {paddingFilesCount} padding files)";
                }

                var totalSize = files.Sum(z => (long)z.Size).ToPrettySize(2);

                Dispatcher.Invoke(() =>
                {
                    if (_disposed)
                        return;

                    fileListView.SetDataContext(root.VisibileChildren);

                    TotalFilesCount.Content = totalFilesCount;

                    TotalSize.Content = $"Total size: {totalSize}";
                });

                LoadPercent = 100d;
            });
        }

        private Dictionary<string, TorrentFileEntry> LoadFromTorrent(string torrentPath)
        {
            var nodeEntries = new Dictionary<string, TorrentFileEntry>();

            var root = new TorrentFileEntry(Path.GetFileName(torrentPath), true);
            nodeEntries.Add(string.Empty, root);

            using (var stream = File.OpenRead(torrentPath))
            using (var reader = new BencodeReader(stream))
            {
                var parser = new TorrentParser();
                var torrent = parser.Parse(reader);

                switch (torrent.FileMode)
                {
                    case TorrentFileMode.Single:
                        ProcessFile(
                            new[] { torrent.File.FileNameUtf8 ?? torrent.File.FileName }, 
                            torrent.File.FileSize, torrent.File.Md5Sum);
                        break;


                    case TorrentFileMode.Multi:
                        foreach (var item in torrent.Files)
                        {
                            ProcessFile(item.PathUtf8 ?? item.Path, item.FileSize, item.Md5Sum);
                        }
                        break;
                }

                var infohash = torrent.OriginalInfoHash.ToLower();

                // must use original infohash, BencodeNET ignore some fields from the torrent
                // see: https://github.com/Krusen/BencodeNET/issues/62
                this._magnetUri = TorrentUtil.CreateMagnetLink(
                    infohash,
                    torrent.DisplayName,
                    torrent.Trackers.SelectMany(x => x),
                    MagnetLinkOptions.IncludeTrackers);

                var torrentName = torrent.DisplayNameUtf8 ?? torrent.DisplayName ?? string.Empty;
                
                this.Dispatcher.Invoke(() =>
                {
                    if (this._disposed)
                        return;

                    this.TorrentName.Content = $"Name: {torrentName}";
                    this.TorrentBtih.Content = $"BTIH: {infohash}";
                });
            }

            void ProcessFile(IList<string> pathList, long fileSize, string md5Sum)
            {
                // process folders. When entry is a directory, all fragments are folders.
                var parts = new List<string>();

                for (var i = 0; i < pathList.Count; i++)
                {
                    var isFolder = i < pathList.Count - 1;
                    var namePart = pathList[i];
                    parts.Add(namePart);
                    var path = Path.Combine(parts.ToArray());
                    if (!nodeEntries.ContainsKey(path))
                    {
                        nodeEntries.TryGetValue(Path.GetDirectoryName(path) ?? "", out var parent);
                        var entry = new TorrentFileEntry(namePart, isFolder, parent, md5Sum);
                        nodeEntries.Add(path, entry);
                        if (!isFolder)
                        {
                            entry.Size = (ulong)Math.Max(0, fileSize);
                        }
                    }
                }
            }

            return nodeEntries;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CopyMagnetUriButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this._magnetUri is string uri)
            {
                try
                {
                    Clipboard.SetText(uri);
                    return;
                }
                catch (Exception) { }
            }

            MessageBox.Show("Failed to copy magnet uri.");
        }
    }
}