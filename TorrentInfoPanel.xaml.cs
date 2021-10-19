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
using System.Windows.Controls;

using BencodeNET.IO;
using BencodeNET.Objects;
using BencodeNET.Parsing;
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
        private readonly Dictionary<string, TorrentFileEntry> _fileEntries = new Dictionary<string, TorrentFileEntry>();
        private bool _disposed;
        private double _loadPercent;
        private ulong _totalZippedSize;
        private string _type;

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
                _totalZippedSize = (ulong)new FileInfo(path).Length;

                var root = new TorrentFileEntry(Path.GetFileName(path), true);
                _fileEntries.Add("", root);

                try
                {
                    LoadFromTorrent(path);
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    Dispatcher.Invoke(() => { lblLoading.Content = "Preview failed. See log for more details."; });
                    return;
                }

                var files = this._fileEntries
                    .Where(z => !z.Value.IsFolder)
                    .Select(z => z.Value)
                    .ToList();

                var paddingFilesCount = files.Count(z => z.IsPaddingFile);
                var totalFilesCount = $"Total {files.Count} files";
                if (paddingFilesCount > 0)
                {
                    totalFilesCount += $" (included {paddingFilesCount} padding files)";
                }

                var totalSize = files.Sum(z => (long)z.Size).ToPrettySize(2);

                Dispatcher.Invoke(() =>
                {
                    if (_disposed)
                        return;

                    fileListView.SetDataContext(_fileEntries[""].Children.Keys);

                    TotalFilesCount.Content = totalFilesCount;
                    archiveSizeC.Content =
                        $"Compressed size {((long)_totalZippedSize).ToPrettySize(2)}";

                    TotalSize.Content = $"Total size: {totalSize}";
                });

                LoadPercent = 100d;
            });
        }

        private void LoadFromTorrent(string torrentPath)
        {
            using (var stream = File.OpenRead(torrentPath))
            using (var reader = new BencodeReader(stream))
            {
                var parser = new TorrentParser();
                var torrent = parser.Parse(reader);

                switch (torrent.FileMode)
                {
                    case TorrentFileMode.Single:
                        ProcessFile(new[] { torrent.File.FileNameUtf8 ?? torrent.File.FileName }, torrent.File.FileSize);
                        break;


                    case TorrentFileMode.Multi:
                        foreach (var item in torrent.Files)
                        {
                            ProcessFile(item.PathUtf8 ?? item.Path, item.FileSize);
                        }
                        break;
                }

                var torrentName = torrent.DisplayNameUtf8 ?? torrent.DisplayName ?? String.Empty;
                var btih = torrent.GetInfoHash();

                this.Dispatcher.Invoke(() =>
                {
                    if (this._disposed)
                        return;

                    this.TorrentName.Content = $"Name: {torrentName}";
                    this.TorrentBtih.Content = $"BTIH: {btih}";
                });
            }

            void ProcessFile(IList<string> pathList, long fileSize)
            {
                // process folders. When entry is a directory, all fragments are folders.
                var parts = new List<string>();

                for (var i = 0; i < pathList.Count; i++)
                {
                    var isFolder = i < pathList.Count - 1;
                    var namePart = pathList[i];
                    parts.Add(namePart);
                    var path = Path.Combine(parts.ToArray());
                    if (!_fileEntries.ContainsKey(path))
                    {
                        _fileEntries.TryGetValue(Path.GetDirectoryName(path) ?? "", out var parent);
                        var afe = new TorrentFileEntry(namePart, isFolder, parent);
                        _fileEntries.Add(path, afe);
                        if (!isFolder)
                        {
                            afe.Size = (ulong)Math.Max(0, fileSize);
                        }
                    }
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}