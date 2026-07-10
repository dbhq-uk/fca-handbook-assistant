using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Domain;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Tests.Ai;

public class LocalGroundedChatClientTests
{
    static string BuildPrompt(params RetrievedProvision[] provisions) =>
        PromptTemplates.BuildUserMessage("What must a firm do?", provisions);

    static RetrievedProvision Retrieved(string reference, string url, string text) =>
        new(new Provision(reference, reference.Split(' ')[0], "title", url, text, "hash"), 0.9);

    [Fact]
    public async Task Answers_by_citing_the_top_provision_from_the_prompt()
    {
        var client = new LocalGroundedChatClient();
        var prompt = BuildPrompt(
            Retrieved("PRIN 2.1.1", "https://handbook.fca.org.uk/PRIN/2/1", "A firm must conduct its business with integrity."),
            Retrieved("SYSC 4.1.1", "https://handbook.fca.org.uk/SYSC/4/1", "A firm must have robust governance arrangements."));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)]);
        var answer = GroundedAnswerJson.TryParse(response.Text);

        Assert.NotNull(answer);
        Assert.False(answer!.Refused);
        Assert.Single(answer.Citations);
        Assert.Equal("PRIN 2.1.1", answer.Citations[0].Reference);
        Assert.Equal("https://handbook.fca.org.uk/PRIN/2/1", answer.Citations[0].Url);
        Assert.Contains("integrity", answer.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refuses_when_the_prompt_carries_no_provisions()
    {
        var client = new LocalGroundedChatClient();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Question:\nHello\n\nProvisions you may cite:")]);
        var answer = GroundedAnswerJson.TryParse(response.Text);

        Assert.NotNull(answer);
        Assert.True(answer!.Refused);
    }
}
