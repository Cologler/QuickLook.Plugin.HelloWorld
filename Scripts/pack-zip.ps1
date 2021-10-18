Remove-Item ..\QuickLook.Plugin.TorrentViewer.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path ..\bin\Release\ -Exclude *.pdb,*.xml
Compress-Archive $files ..\QuickLook.Plugin.TorrentViewer.zip
Move-Item ..\QuickLook.Plugin.TorrentViewer.zip ..\QuickLook.Plugin.TorrentViewer.qlplugin