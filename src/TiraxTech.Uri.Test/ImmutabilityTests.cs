using System.Collections.Immutable;

namespace TiraxTech.UriTest;

// Regression test for the "Immutable URI" contract (defect #6):
// Paths must not be an externally-mutable array.
public class ImmutabilityTests
{
    [Test]
    [DisplayName("RelativeUri.Paths is exposed as an immutable collection")]
    public async Task PathsIsImmutable()
    {
        var prop = typeof(RelativeUri).GetProperty(nameof(RelativeUri.Paths))!;
        await Assert.That(prop.PropertyType).IsEqualTo(typeof(ImmutableArray<string>));
    }
}
