namespace CheckinToPlm.Tests;

public class SyncPlannerTests
{
    // Assembly A (iteration 10) contains part B (20) and subassembly C (30); C contains part D (40).
    private static readonly ItemRef A = new(1, 10, "A");
    private static readonly ItemRef B = new(2, 20, "B");
    private static readonly ItemRef C = new(3, 30, "C");
    private static readonly ItemRef D = new(4, 40, "D");

    private static readonly BomLink[] Links =
    [
        new(10, 20, 2),
        new(10, 30, 3),
        new(30, 40, 4),
    ];

    [Fact]
    public void Assembly_checkin_syncs_only_the_top_item_with_all_descendants()
    {
        var plan = SyncPlanner.Plan([A, B, C, D], Links, syncBom: true);

        var request = Assert.Single(plan);
        Assert.Equal(A, request.Item);
        Assert.Equal([20L, 30L, 40L], request.AdditionalEntities.Order());
    }

    [Fact]
    public void Single_part_without_bom_syncs_alone()
    {
        var plan = SyncPlanner.Plan([B], [], syncBom: true);

        var request = Assert.Single(plan);
        Assert.Equal(B, request.Item);
        Assert.Empty(request.AdditionalEntities);
    }

    [Fact]
    public void Unrelated_items_each_get_their_own_task()
    {
        var other = new ItemRef(9, 90, "X");

        var plan = SyncPlanner.Plan([C, D, other], [new BomLink(30, 40, 4)], syncBom: true);

        Assert.Equal(["C", "X"], plan.Select(r => r.Item.Number).Order());
        Assert.Equal([40L], plan.Single(r => r.Item.Number == "C").AdditionalEntities);
    }

    [Fact]
    public void Shared_child_is_listed_once()
    {
        // B is used directly under A and again under C.
        BomLink[] links = [new(10, 20, 2), new(10, 30, 3), new(30, 20, 2)];

        var plan = SyncPlanner.Plan([A], links, syncBom: true);

        Assert.Equal([20L, 30L], Assert.Single(plan).AdditionalEntities.Order());
    }

    [Fact]
    public void Properties_only_mode_syncs_every_item_without_children()
    {
        var plan = SyncPlanner.Plan([A, B, C, D], Links, syncBom: false);

        Assert.Equal(4, plan.Count);
        Assert.All(plan, r => Assert.Empty(r.AdditionalEntities));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("not json", true)]
    [InlineData("{}", true)]
    [InlineData("""{"Workflows":[{"Type":"Adsk.UploadItem","Settings":[]}]}""", true)]
    [InlineData("""{"Workflows":[{"Type":"Adsk.UploadItem","Settings":[{"Name":"ItemSyncActionSetting","Value":1}]}]}""", true)]
    [InlineData("""{"Workflows":[{"Type":"Adsk.UploadItem","Settings":[{"Name":"ItemSyncActionSetting","Value":2}]}]}""", false)]
    [InlineData("""{"workflows":[{"type":"adsk.uploaditem","settings":[{"name":"itemSyncActionSetting","value":0}]}]}""", false)]
    [InlineData("""{"Workflows":[{"Type":"Adsk.UploadFile","Settings":[{"Name":"ItemSyncActionSetting","Value":2}]}]}""", true)]
    public void SyncItemAndBom_reads_the_connector_setting(string? content, bool expected)
    {
        Assert.Equal(expected, SyncPlanner.SyncItemAndBom(content));
    }
}
