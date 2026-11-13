using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace CheckinToPlm;

/// <summary>The API equivalent of Vault's "Assign/Update Item" command.</summary>
internal static class ItemAssigner
{
    /// <summary>Creates or updates items for the given file iterations and returns the committed items.</summary>
    public static Item[] Assign(WebServiceManager manager, IReadOnlyList<long> fileIds)
    {
        var service = manager.ItemService;
        Item[] edited = [];
        try
        {
            service.AddFilesToPromote(fileIds.ToArray(), ItemAssignAll.Default, true);
            var order = service.GetPromoteComponentOrder(out var timestamp);
            if (order.PrimaryArray is { Length: > 0 } primary)
            {
                service.PromoteComponents(timestamp, primary);
            }

            if (order.NonPrimaryArray is { Length: > 0 } nonPrimary)
            {
                service.PromoteComponentLinks(nonPrimary);
            }

            var results = service.GetPromoteComponentsResults(timestamp);
            edited = results.ItemRevArray ?? [];
            if (edited.Length == 0)
            {
                return [];
            }

            service.UpdateAndCommitItems(edited);
        }
        catch
        {
            Rollback(service, edited);
            throw;
        }

        return service.GetLatestItemsByItemMasterIds(edited.Select(i => i.MasterId).Distinct().ToArray()) ?? [];
    }

    private static void Rollback(ItemService service, Item[] edited)
    {
        try
        {
            if (edited.Length > 0)
            {
                service.UndoEditItems(edited.Select(i => i.Id).ToArray());
            }

            service.DeleteUncommittedItems(false);
        }
        catch (Exception ex)
        {
            Log.Error("Rollback after a failed item assignment did not complete", ex);
        }
    }
}
