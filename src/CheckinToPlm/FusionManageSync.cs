using System.Globalization;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace CheckinToPlm;

/// <summary>
/// Queues the same Vault Connector task that the native "Sync to Fusion Manage" command queues.
/// Values mirror Connectivity.Explorer.Item.ExtSyncCommand in Vault 2027.
/// </summary>
internal static class FusionManageSync
{
    public const string ConfigId = "Adsk.Vault.ExternalSyncTask.FusionManage";
    public const string UploadItemWorkflow = "Adsk.UploadItem";
    private const string ItemEntityClassId = "ITEM";

    /// <summary>Returns the queued tasks. Throws <see cref="InvalidOperationException"/> when the connector is not usable.</summary>
    public static List<ExtSyncTask> Queue(WebServiceManager manager, Item[] items)
    {
        var sync = manager.ExternalSyncService;
        if (!sync.GetExtSyncEnabled())
        {
            throw new InvalidOperationException("The Vault Connector is not enabled on this server.");
        }

        var config = sync.GetExtSyncConfig(ConfigId);
        if (string.IsNullOrEmpty(config?.Content))
        {
            throw new InvalidOperationException("The Vault Connector for Fusion Manage has not been configured.");
        }

        var refs = items.Select(i => new ItemRef(i.MasterId, i.Id, i.ItemNum)).ToList();
        var syncBom = SyncPlanner.SyncItemAndBom(config.Content);
        var links = new List<BomLink>();
        if (syncBom)
        {
            var associations = manager.ItemService.GetItemBOMAssociationsByItemIds(refs.Select(r => r.IterationId).ToArray(), true) ?? [];
            links.AddRange(associations.Select(a => new BomLink(a.ParItemID, a.CldItemID, a.CldItemMasterID)));
        }

        var tasks = new List<ExtSyncTask>();
        foreach (var request in SyncPlanner.Plan(refs, links, syncBom))
        {
            NameValuePair[]? parameters = request.AdditionalEntities.Count == 0
                ? null
                :
                [
                    new NameValuePair
                    {
                        Name = "AdditionalEntities",
                        Val = string.Join(",", request.AdditionalEntities.Select(id => id.ToString(CultureInfo.InvariantCulture))),
                    },
                ];

            tasks.Add(sync.AddExtSyncTask(
                request.Item.IterationId,
                ItemEntityClassId,
                config.Id,
                UploadItemWorkflow,
                $"Sync to Fusion Manage ({request.Item.Number})",
                parameters));
        }

        return tasks;
    }
}
