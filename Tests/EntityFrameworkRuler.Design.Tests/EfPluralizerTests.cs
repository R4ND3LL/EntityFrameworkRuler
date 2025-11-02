using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Shouldly;
using Xunit;

namespace EntityFrameworkRuler.Design.Tests;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class EfPluralizerTests {
    [Fact]
    public void Pluralize_works()
        => Assert.Equal("Tests", new RuledPluralizer().Pluralize("Test"));

    [Fact]
    public void Singularize_works()
        => Assert.Equal("Test", new RuledPluralizer().Singularize("Tests"));

    [Fact]
    public void TrickyWordsShouldPluralize() {
        var words = new[] { ("Lens", "Lens", "Len") };

        var pluralizers = new IPluralizer[] {
            new HumanizerPluralizer(), // used by EF
            //new Bricelam.EntityFrameworkCore.Design.Pluralizer(), // used by EFPT. commented out because this will fail
        };
        foreach (var wordTuple in words) {
            var word = wordTuple.Item1;
            var correctPluralizedForm = wordTuple.Item2;
            var correctSingularizedForm = wordTuple.Item3;

            foreach (var pluralizer in pluralizers) {
                var pluralize = pluralizer.Pluralize(word);
                var singularized = pluralizer.Singularize(word);

                pluralize.ShouldBe(correctPluralizedForm);
                singularized.ShouldBe(correctSingularizedForm);
            }
        }
    }
}