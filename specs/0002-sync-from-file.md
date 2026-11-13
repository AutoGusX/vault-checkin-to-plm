# 0002: Sync to Fusion Manage from a file

## Problem

The native "Sync to Fusion Manage" command only exists on items. A user looking at a file in the Vault client has to find its item first, then right-click and sync.

## Goal

Right-click one or more files in the Vault client → **Sync to Fusion Manage**. The extension finds the items tied to the selected files and queues the same sync task as the native item command.

## Behavior

1. **Command.** "Sync to Fusion Manage" in the file context menu of the Vault client. Works on one or many selected files (file or file-version selections).
2. **Find items.** For each selected file, take its latest version and read every item linked to it (primary and secondary links). Items are de-duplicated across the selection.
3. **Files without an item.** If any selected file has no item linked to its latest version (never assigned, or the item is behind the file), ask once: "Assign/update items for these files and sync?" Yes runs the same assign step as spec 0001 and adds the resulting items. No leaves those files out.
4. **Choose items.**
   - Exactly one item in total: sync it straight away, no dialog.
   - More than one: show a list (item number, title, linked file) with every row ticked, plus Select all and Clear. The user syncs one, many, or all.
5. **Queue.** Selected items go through the same path as spec 0001 (`FusionManageSync.Queue`), so the connector checks, BOM handling, and parent/child de-duplication match the native command.
6. **Feedback.** Success: a tray notification with the number of tasks queued. Nothing to sync, connector not enabled or configured, or a failure: a message box, since the user asked for the action directly.

## Notes

- The command does not depend on the check-in settings from spec 0001; it works with the check-in automation switched off.
- Vault client only. CAD add-ins already expose the native item sync.

## Verification

- File with one item → task queued with no dialog; connector log shows Succeeded.
- File linked to several items → picker lists them all; syncing a subset queues only those.
- Multi-select of files → combined, de-duplicated list.
- File with no item → prompt; Yes creates the item and syncs; No queues nothing for that file.
- Connector disabled → message box, nothing queued.
