using FcaHandbookAssistant.Core.Ingestion;

namespace FcaHandbookAssistant.Tests.Ingestion;

public class ProvisionChunkerTests
{
    const string Html =
        "<html><head><style>.x{color:red}</style></head><body>" +
        "<h3>PRIN 2.1.1</h3><p>A firm must conduct its business with <b>integrity</b>.</p>" +
        "<script>track()</script></body></html>";

    [Fact]
    public void ToCleanText_strips_tags_scripts_and_styles()
    {
        var text = ProvisionChunker.ToCleanText(Html);

        Assert.Contains("A firm must conduct its business with integrity.", text);
        Assert.DoesNotContain("track()", text);
        Assert.DoesNotContain(".x{", text);
        Assert.DoesNotContain("<", text);
    }

    [Fact]
    public void ToCleanText_decodes_entities_and_collapses_whitespace()
    {
        var text = ProvisionChunker.ToCleanText("<p>fair,&nbsp;clear   and\n\n not&nbsp;misleading</p>");

        Assert.Equal("fair, clear and not misleading", text);
    }

    [Fact]
    public void ContentHash_is_stable_and_differs_by_content()
    {
        Assert.Equal(ProvisionChunker.ContentHash("abc"), ProvisionChunker.ContentHash("abc"));
        Assert.NotEqual(ProvisionChunker.ContentHash("abc"), ProvisionChunker.ContentHash("abd"));
    }
}
