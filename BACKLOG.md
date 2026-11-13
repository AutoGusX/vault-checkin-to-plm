# Backlog

Specs: [0001 auto item sync](specs/0001-auto-item-sync.md), [0002 sync from a file](specs/0002-sync-from-file.md)

## Spec 0002: sync from a file

Built and deployed 2026-11-13; used live in the Vault client the same day.

- [x] "Sync to Fusion Manage" shows in the file right-click menu.
- [x] File with one item: task queued with no dialog.
- [x] File tied to several items: picker lists them all; syncing a subset queues only those.
- [ ] **Bug:** the assign-first path failed twice with Vault error 1387 (restrictions) on commit, and the rollback failed the same way. Log the restriction detail and show it to the user.
- [ ] Several files selected: combined, de-duplicated list.
- [ ] File with no item: prompt; Yes creates the item and syncs; No queues nothing.
- [ ] Connector disabled: message box, nothing queued.
- [ ] Confirm `GetItemsByFileId` on the latest file version finds secondary-linked items (for example a drawing tied to its model's item).

## Spec 0001: live checks owed

- [ ] **Check-in from the Vault client** (only Inventor has been confirmed end to end).
- [ ] **Assign only.** Untick sync: item but no sync task. Untick both: nothing happens.
- [ ] **Failure path.** Connector disabled or item outside the sync rules: check-in still succeeds, failure logged, tray notification shown.
- [ ] **AutoCAD and SolidWorks check-ins.** Confirm the hook loads in those add-ins.

## Open questions

- Does the tray notification show reliably from inside Inventor?
- `AddFilesToPromote` uses `ItemAssignAll.Default` (follows the vault's own assign-item configuration). Revisit if child items are not created as expected.

## Later

- Unit tests write to the real log file; point them at a temp log.
- Installer and docs for sharing; toggle in Inventor's ribbon; Vault 2026.

## Done

- [x] Repo, spec, project scaffold, deploy script.
- [x] Check-in handler, batching, assign item, Fusion Manage sync (mirrors the native command), settings dialog, tray notification, unit tests.
- [x] Hook loads in the Vault client and in Inventor (2026-11-13).
- [x] Settings dialog saves from the Vault client (2026-11-13).
- [x] Part check-in from Inventor: item assigned, sync task succeeded on the connector (2026-11-13).
- [x] Assembly check-in from Inventor: 19 files, 19 items, one sync task for the top item (2026-11-13).
- [x] Published as a public repository under the MIT license (2026-11-13).
- [x] Right-click "Sync to Fusion Manage" on files with an item picker (2026-11-13).
