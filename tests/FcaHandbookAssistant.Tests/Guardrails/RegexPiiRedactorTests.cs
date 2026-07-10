using FcaHandbookAssistant.Core.Guardrails;

namespace FcaHandbookAssistant.Tests.Guardrails;

public class RegexPiiRedactorTests
{
    readonly IPiiRedactor _redactor = new RegexPiiRedactor();

    [Fact]
    public void Redacts_email()
    {
        var r = _redactor.Redact("You can reach me at jane.doe@example.co.uk about this.");

        Assert.DoesNotContain("jane.doe@example.co.uk", r.Redacted);
        Assert.Contains("[REDACTED:EMAIL]", r.Redacted);
        Assert.Contains("EMAIL", r.Kinds);
    }

    [Theory]
    [InlineData("My number is 07911 123456.")]
    [InlineData("Call +44 7911 123456 today.")]
    public void Redacts_uk_phone(string input)
    {
        var r = _redactor.Redact(input);

        Assert.Contains("[REDACTED:PHONE]", r.Redacted);
        Assert.Contains("PHONE", r.Kinds);
    }

    [Fact]
    public void Redacts_long_account_number()
    {
        var r = _redactor.Redact("Account 12345678 was debited.");

        Assert.DoesNotContain("12345678", r.Redacted);
        Assert.Contains("[REDACTED:NUMBER]", r.Redacted);
    }

    [Fact]
    public void Redacts_national_insurance_number()
    {
        var r = _redactor.Redact("NI number QQ123456C is on file.");

        Assert.DoesNotContain("QQ123456C", r.Redacted);
        Assert.Contains("[REDACTED:NINO]", r.Redacted);
    }

    [Fact]
    public void Leaves_plain_compliance_text_unchanged()
    {
        const string input = "What does PRIN 2.1.1 require of firms?";

        var r = _redactor.Redact(input);

        Assert.Equal(input, r.Redacted);
        Assert.Empty(r.Kinds);
    }
}
