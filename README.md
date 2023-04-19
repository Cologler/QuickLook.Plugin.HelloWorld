![QuickLook icon](https://user-images.githubusercontent.com/1687847/29485863-8cd61b7c-84e2-11e7-97d5-eacc2ba10d28.png)

# QuickLook.Plugin.TorrentViewer

## How-To-Use

1. Install plugin.
2. Modify `QuickLook.exe.config` and add the following lines into `configuration/runtime` section:

``` xml
<configuration>
  <runtime>

    ...

    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Buffers" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.0.3.0" newVersion="4.0.3.0"/>
      </dependentAssembly>
    </assemblyBinding>

    ...

  </runtime>
</configuration>
```

Restart `QuickLook.exe`, it should support to preview torrent file now.

### Why the modify is necessary?

Because of the `BencodeNET` is target to the `.Net Standard 2.0` (with `System.Buffers 4.0.2.0`),
but the `QuickLook` is target to the `.Net Framework 4.6.2` (with `System.Buffers 4.0.3.0`);

Also see:

- https://github.com/dotnet/runtime/issues/1830
- https://github.com/dotnet/runtime/issues/27774

### Where is the `QuickLook.exe.config`?

It should nearby your `QuickLook.exe` file.

### Unable to modify the Microsoft store version of `QuickLook.exe.config`

https://github.com/Cologler/QuickLook.Plugin.TorrentViewer/issues/3 reported this, 
but I have no idea how to solve it. ¯\\_(ツ)_/¯

