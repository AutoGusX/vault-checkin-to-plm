using System.Text.Json;

namespace CheckinToPlm;

internal readonly record struct ItemRef(long MasterId, long IterationId, string Number);

internal readonly record struct BomLink(long ParentIterationId, long ChildIterationId, long ChildMasterId);

internal sealed record SyncRequest(ItemRef Item, IReadOnlyList<long> AdditionalEntities);

/// <summary>
/// Pure logic that mirrors what Vault's own "Sync to Fusion Manage" command does
/// before it queues tasks (read from Connectivity.Explorer.Item.ExtSyncCommand, Vault 2027).
/// </summary>
internal static class SyncPlanner
{
    /// <summary>
    /// With BOM sync on: items that are a BOM child of another item in the set are dropped,
    /// and each remaining item carries every descendant iteration ID.
    /// With BOM sync off: every item syncs on its own.
    /// </summary>
    public static IReadOnlyList<SyncRequest> Plan(IReadOnlyList<ItemRef> items, IReadOnlyList<BomLink> links, bool syncBom)
    {
        if (!syncBom)
        {
            return items.Select(i => new SyncRequest(i, [])).ToList();
        }

        var rootMasterIds = items.Select(i => i.MasterId).ToHashSet();
        var children = new Dictionary<long, HashSet<long>>();
        foreach (var link in links)
        {
            rootMasterIds.Remove(link.ChildMasterId);
            if (!children.TryGetValue(link.ParentIterationId, out var set))
            {
                set = [];
                children[link.ParentIterationId] = set;
            }

            set.Add(link.ChildIterationId);
        }

        var requests = new List<SyncRequest>();
        foreach (var root in items.Where(i => rootMasterIds.Contains(i.MasterId)))
        {
            var descendants = new List<long>();
            var seen = new HashSet<long> { root.IterationId };
            var stack = new Stack<long>();
            stack.Push(root.IterationId);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!children.TryGetValue(current, out var set))
                {
                    continue;
                }

                foreach (var child in set)
                {
                    if (seen.Add(child))
                    {
                        descendants.Add(child);
                        stack.Push(child);
                    }
                }
            }

            requests.Add(new SyncRequest(root, descendants));
        }

        return requests;
    }

    /// <summary>
    /// Reads the connector configuration JSON: the "Adsk.UploadItem" workflow's
    /// "ItemSyncActionSetting" is 1 for "Item &amp; BOM". Missing or unreadable means BOM sync is on,
    /// which is Vault's own default.
    /// </summary>
    public static bool SyncItemAndBom(string? configContent)
    {
        if (string.IsNullOrWhiteSpace(configContent))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(configContent);
            if (!TryGet(document.RootElement, "Workflows", out var workflows) || workflows.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            foreach (var workflow in workflows.EnumerateArray())
            {
                if (!TryGet(workflow, "Type", out var type) ||
                    !string.Equals(type.ToString(), FusionManageSync.UploadItemWorkflow, StringComparison.OrdinalIgnoreCase) ||
                    !TryGet(workflow, "Settings", out var settings) || settings.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var setting in settings.EnumerateArray())
                {
                    if (TryGet(setting, "Name", out var name) &&
                        string.Equals(name.ToString(), "ItemSyncActionSetting", StringComparison.OrdinalIgnoreCase) &&
                        TryGet(setting, "Value", out var value) && value.ValueKind == JsonValueKind.Number)
                    {
                        return value.GetInt32() == 1;
                    }
                }

                return true;
            }
        }
        catch (JsonException)
        {
        }

        return true;
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
