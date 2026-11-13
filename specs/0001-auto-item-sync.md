# 0001: Auto assign item and sync to Fusion Manage after check-in

## Problem

In Vault Professional 2027, getting a design into Fusion Manage takes three manual steps: check in the file, run Assign/Update Item, then run Sync to Fusion Manage on the item.

## Goal

A Vault extension that runs steps 2 and 3 automatically after every normal check-in, controlled by a per-user checkbox that stays set. Works from the Vault client and from Inventor. Demo machine first.

## Behavior

1. **Check-in event.** An `IWebServiceExtension` subscribes to `DocumentService.CheckinFileEvents.Post`. It reads settings fresh on each event. If the feature is off, the host is the Job Processor, or the file type is out of scope, it does nothing. The handler never blocks or throws into the check-in.
2. **Batching.** One assembly check-in fires one event per file. File IDs are collected and processed once after about 3 seconds of quiet, on a background thread, using the session's `WebServiceManager`.
3. **Assign items.** `AddFilesToPromote` → `GetPromoteComponentOrder` → `PromoteComponents` → `GetPromoteComponentsResults` → `UpdateAndCommitItems`; roll back with `UndoEditItems`/`DeleteUncommittedItems` on error.
4. **Sync.** If enabled and the connector is on and configured, queue one sync task per top-level item in the batch, mirroring the native command:
   `ExternalSyncService.AddExtSyncTask(itemIterationId, entityClassId, GetExtSyncConfig("Adsk.Vault.ExternalSyncTask.FusionManage").Id, "Adsk.UploadItem", "Sync to Fusion Manage (<number>)", [AdditionalEntities = child item iteration IDs])`.
5. **Feedback.** Log every step to `%LOCALAPPDATA%\CheckinToPlm\log.txt`. Failures raise a non-blocking tray notification.

## Settings

Tools → "Check-in to PLM settings…" in the Vault client. Stored in `%APPDATA%\CheckinToPlm\settings.json`.

- After check-in, assign/update item (default on)
- Then sync the item to Fusion Manage (default on)
- File types (default `.ipt .iam .dwg .sldprt .sldasm`)

## Constraints

- Vault 2027 only (.NET 10). `ExternalSyncService` is public but undocumented and may change with a Vault update.
- The native check-in dialog cannot be extended, so the checkbox lives in a settings dialog.

## Not in v1

Installer and docs for sharing, a toggle in Inventor's ribbon, Vault Office, Vault 2026.

## Verification

See `BACKLOG.md` for the per-step live checks.
