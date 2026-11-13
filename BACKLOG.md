# Backlog

Spec: [specs/0001-auto-item-sync.md](specs/0001-auto-item-sync.md)

## Live checks owed

- [ ] **Check-in from the Vault client** (only Inventor has been confirmed end to end).
- [ ] **Assign only.** Untick sync → item but no sync task. Untick both → nothing happens.
- [ ] **Assembly from Inventor.** One sync task for the top item; BOM present in Fusion Manage.
- [ ] **Failure path.** Connector disabled or item outside the sync rules → check-in still succeeds, failure logged, tray notification shown.
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
- [x] Published as a public repository under the MIT license (2026-11-13).
