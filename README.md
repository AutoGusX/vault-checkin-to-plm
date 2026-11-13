# Vault check-in to PLM

A Vault Professional 2027 extension that turns three steps into one. After a normal check-in it runs Assign/Update Item on the checked-in files, then queues the Vault Connector's sync to Fusion Manage for the resulting item.

> **Unsupported sample.** This is not an Autodesk product and comes with no support. The sync step calls `ExternalSyncService`, a Vault 2027 web service that is public in the API but not documented, so a Vault update may break it. Try it on a test vault first.

## What it does

1. You check in from the Vault client or a CAD add-in, exactly as before.
2. The extension waits for the check-in to settle (an assembly check-in arrives as a burst of files), then assigns or updates items for the files in scope.
3. It queues one "Sync to Fusion Manage" task per top-level item, with its BOM, the same way the built-in command does.

A settings dialog (**Tools > Check-in to PLM settings...** in the Vault client) holds the checkboxes. The choice is saved per Windows user and also applies to check-ins from CAD add-ins.

- After check-in, assign/update item
- Then sync the item to Fusion Manage
- File types (default `.ipt .iam .dwg .sldprt .sldasm`)

## Sync straight from a file

In the Vault client, right-click one or more files and choose **Sync to Fusion Manage**. The extension finds the items tied to those files, so there is no need to open the item first.

- One item: it syncs straight away.
- Several items: a list appears with every item ticked; sync one, many, or all.
- No up-to-date item: you are asked whether to assign/update the item first.

## Requirements

- Vault Professional 2027 client (the extension targets .NET 10 and the 2027 API)
- The Vault Connector for Fusion Manage enabled and configured on the Vault server
- .NET 10 SDK to build

## Build and install

```powershell
.\deploy.ps1
```

This builds the project against the Vault client's own assemblies in `C:\Program Files\Autodesk\Vault Client 2027\Explorer` and copies the result to `C:\ProgramData\Autodesk\Vault 2027\Extensions\CheckinToPlm`. Restart the Vault client and any CAD application afterwards; extensions load at startup.

To remove it, delete that folder and restart.

## Checking that it worked

- Extension log: `%LOCALAPPDATA%\CheckinToPlm\log.txt`
- Settings file: `%APPDATA%\CheckinToPlm\settings.json`
- Connector log on the Vault server: `C:\ProgramData\Autodesk\VaultServer\FileStore\VaultConnectorLog-<date>.txt`
- In Vault, the item's Fusion Manage Sync Status property

A failed assign or sync never blocks the check-in. It is logged and raises a tray notification.

## Tested so far

- Check-in from Inventor 2027: item assigned and synced to Fusion Manage.
- Settings dialog in the Vault client.

Not yet tested: the right-click sync from a file, AutoCAD and SolidWorks check-ins, multi-level assemblies, and the failure paths. See [BACKLOG.md](BACKLOG.md).

## Tests

```powershell
dotnet test tests\CheckinToPlm.Tests
```

## License

MIT. See [LICENSE](LICENSE).
