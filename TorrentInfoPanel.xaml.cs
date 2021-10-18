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
            new Task(() =>
            {
                _totalZippedSize = (ulong)new FileInfo(path).Length;

                var root = new TorrentFileEntry(Path.GetFileName(path), true);
                _fileEntries.Add("", root);

                try
                {
                    LoadItemsFromArchive(path);
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    Dispatcher.Invoke(() => { lblLoading.Content = "Preview failed. See log for more details."; });
                    return;
                }

                var folders = -1; // do not count root node
                var files = 0;
                ulong sizeU = 0L;
                _fileEntries.ForEach(e =>
                {
                    if (e.Value.IsFolder)
                        folders++;
                    else
                        files++;

                    sizeU += e.Value.Size;
                });

                string t;
                var d = folders != 0 ? $"{folders} folders" : string.Empty;
                var f = files != 0 ? $"{files} files" : string.Empty;
                if (!string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(f))
                    t = $", {d} and {f}";
                else if (string.IsNullOrEmpty(d) && string.IsNullOrEmpty(f))
                    t = string.Empty;
                else
                    t = $", {d}{f}";

                Dispatcher.Invoke(() =>
                {
                    if (_disposed)
                        return;

                    fileListView.SetDataContext(_fileEntries[""].Children.Keys);
                    archiveCount.Content =
                        $"{_type} archive{t}";
                    archiveSizeC.Content =
                        $"Compressed size {((long)_totalZippedSize).ToPrettySize(2)}";
                    archiveSizeU.Content = $"Uncompressed size {((long)sizeU).ToPrettySize(2)}";
                });

                LoadPercent = 100d;
            }).Start();
        }

        private void LoadItemsFromArchive(string torrentPath)
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