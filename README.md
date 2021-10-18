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
