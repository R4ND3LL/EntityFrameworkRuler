using EdmxRuler.Common;
using EdmxRuler.Generator.Services;
using Microsoft.EntityFrameworkCore.Design;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace EntityFrameworkRuler.Services {
    /// <summary> Pluralization service override to be used by Ef scaffold process. </summary>
    public class EfRulerPluralizer : IPluralizer {
        private readonly HumanizerPluralizer pluralizer;

        /// <summary> Creates a pluralization service to be used by Ef scaffold process. </summary>
        public EfRulerPluralizer() {
            pluralizer = new HumanizerPluralizer();
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