using EntityFrameworkRuler.Services;
using Xunit;

namespace EntityFrameworkRuler {
    public class EfPluralizerTests {
        [Fact]
        public void Pluralize_works()
            => Assert.Equal("Tests", new EfRulerPluralizer().Pluralize("Test"));

        [Fact]
        public void Singularize_works()
            => Assert.Equal("Test", new EfRulerPluralizer().Singularize("Tests"));
    }
}