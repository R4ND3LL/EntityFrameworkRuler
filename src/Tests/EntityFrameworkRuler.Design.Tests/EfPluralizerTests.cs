using EntityFrameworkRuler.Design.Services;
using Xunit;

namespace EntityFrameworkRuler.Design.Tests;

public class EfPluralizerTests {
    [Fact]
    public void Pluralize_works()
        => Assert.Equal("Tests", new RuledPluralizer().Pluralize("Test"));

    [Fact]
    public void Singularize_works()
        => Assert.Equal("Test", new RuledPluralizer().Singularize("Tests"));
}