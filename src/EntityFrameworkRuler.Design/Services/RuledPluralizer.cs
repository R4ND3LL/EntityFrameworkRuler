using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace EntityFrameworkRuler.Design.Services {
    /// <summary> Pluralization service override to be used by Ef scaffold process. </summary>
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
    public class RuledPluralizer : IPluralizer {
        private readonly HumanizerPluralizer pluralizer;

        /// <summary> Creates a pluralization service to be used by Ef scaffold process. </summary>
        public RuledPluralizer() {
            pluralizer = new();
        }

        /// <summary>
        ///     Gets the plural version of the given identifier. Returns the same
        ///     identifier if it is already pluralized.
        /// </summary>
        /// <param name="identifier">The identifier to be pluralized.</param>
        /// <returns>The pluralized identifier.</returns>
        public virtual string Pluralize(string identifier) {
            return pluralizer.Pluralize(identifier);
        }

        /// <summary>
        ///     Gets the singular version of the given identifier. Returns the same
        ///     identifier if it is already singularized.
        /// </summary>
        /// <param name="identifier">The identifier to be singularized.</param>
        /// <returns>The singularized identifier.</returns>
        public virtual string Singularize(string identifier) {
            return pluralizer.Singularize(identifier);
        }
    }
}