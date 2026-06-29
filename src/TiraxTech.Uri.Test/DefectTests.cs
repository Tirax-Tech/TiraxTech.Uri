using System.Collections;

namespace TiraxTech.UriTest;

// Regression tests for small defects (#12 ICollection null-drop, #9 password redaction).
public class DefectTests
{
    [Test]
    [DisplayName("UpdateQuery(ICollection) keeps null elements as \"null\" (#12)")]
    public async Task UpdateQueryICollectionKeepsNullElements()
    {
        var r = RelativeUri.From("/p").UpdateQuery("k", new ArrayList { "a", null, "b" });
        await Assert.That(r.Query("k")!.Value.ToArray()).IsEquivalentTo(new string?[] { "a", "null", "b" });
    }

    [Test]
    [DisplayName("UriCredentials.ToString() redacts the password (#9)")]
    public async Task CredentialsToStringRedactsPassword()
    {
        var creds = Uri.From("https://alice:s3cr3t@example.com/app").Unwrap().Credentials!;
        var text = creds.ToString();
        await Assert.That(text).DoesNotContain("s3cr3t");
        await Assert.That(text).Contains("alice");
    }
}
