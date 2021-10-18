using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.TorrentViewer
{
    public class Plugin : IViewer
    {
        private TorrentInfoPanel _panel;

        public int Priority => 0;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return File.Exists(path) && path.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size {Width = 800, Height = 400};
        }

        public void View(string path, ContextObject context)
        {
            _panel = new TorrentInfoPanel(path);

            context.ViewerContent = _panel;
            context.Title = $"{Path.GetFileName(path)}";

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            _panel?.Dispose();
            _panel = null;
        }
    }
}