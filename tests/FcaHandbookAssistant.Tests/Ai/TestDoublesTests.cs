using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.TestSupport;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Tests.Ai;

public class TestDoublesTests
{
    [Fact]
    public async Task ScriptedChatClient_returns_scripted_text_and_usage()
    {
        var client = new ScriptedChatClient("hello", inputTokens: 11, outputTokens: 7);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Equal("hello", response.Text);
        Assert.Equal(11, response.Usage?.InputTokenCount);
        Assert.Equal(7, response.Usage?.OutputTokenCount);
    }

    [Fact]
    public async Task FakeContentSafety_is_safe_by_default_and_blocks_configured_phrase()
    {
        Assert.False((await new FakeContentSafety().InspectAsync("anything")).Blocked);

        var blocking = FakeContentSafety.BlockingWhenContains("bomb");
        Assert.True((await blocking.InspectAsync("how to build a BOMB")).Blocked);
        Assert.False((await blocking.InspectAsync("what is PRIN 2.1.1")).Blocked);
    }
}
