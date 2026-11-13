namespace CheckinToPlm.Tests;

public class SettingsTests
{
    [Theory]
    [InlineData("Bracket.ipt", true)]
    [InlineData("Frame.IAM", true)]
    [InlineData("layout.dwg", true)]
    [InlineData("housing.SLDPRT", true)]
    [InlineData("gearbox.sldasm", true)]
    [InlineData("Bracket.idw", false)]
    [InlineData("notes.docx", false)]
    [InlineData("noextension", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Default_scope_covers_inventor_autocad_and_solidworks_models(string? fileName, bool expected)
    {
        Assert.Equal(expected, new Settings().IsInScope(fileName));
    }

    [Fact]
    public void Extensions_without_a_dot_still_match()
    {
        var settings = new Settings { FileExtensions = ["IDW", " .ipn "] };

        Assert.True(settings.IsInScope("sheet.idw"));
        Assert.True(settings.IsInScope("explode.ipn"));
        Assert.False(settings.IsInScope("part.ipt"));
    }

    [Fact]
    public void ParseExtensions_normalizes_and_dedupes()
    {
        Assert.Equal([".ipt", ".iam", ".dwg"], Settings.ParseExtensions("ipt, .IAM; dwg  .ipt"));
    }

    [Fact]
    public void Missing_file_gives_defaults_with_both_options_on()
    {
        var settings = Settings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        Assert.True(settings.AssignItem);
        Assert.True(settings.SyncToFusionManage);
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        var path = Path.Combine(Path.GetTempPath(), "CheckinToPlmTests", Guid.NewGuid() + ".json");
        try
        {
            new Settings { AssignItem = false, SyncToFusionManage = false, FileExtensions = [".ipt"] }.Save(path);
            var loaded = Settings.Load(path);

            Assert.False(loaded.AssignItem);
            Assert.False(loaded.SyncToFusionManage);
            Assert.Equal([".ipt"], loaded.FileExtensions);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.True(Settings.Load(path).AssignItem);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
