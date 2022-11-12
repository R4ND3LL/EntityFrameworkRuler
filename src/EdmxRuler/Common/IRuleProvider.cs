using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EdmxRuler.Applicator;
using EdmxRuler.Rules;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EdmxRuler.Rules.PropertyTypeChanging;

namespace EdmxRuler.Common;

public interface IRuleProvider {
    PrimitiveNamingRules GetPrimitiveNamingRules();
    PropertyTypeChangingRules GetPropertyTypeChangingRules();
    NavigationNamingRules GetNavigationNamingRules();
}

public class DefaultRuleProvider : IRuleProvider {
    private readonly IServiceProvider serviceProvider;

    public DefaultRuleProvider(IServiceProvider serviceProvider) {
        this.serviceProvider = serviceProvider;
    }

    private LoadRulesResponse response;

    public PrimitiveNamingRules GetPrimitiveNamingRules() {
        return GetRules().OfType<PrimitiveNamingRules>().FirstOrDefault();
    }

    public PropertyTypeChangingRules GetPropertyTypeChangingRules() {
        return GetRules().OfType<PropertyTypeChangingRules>().FirstOrDefault();
    }

    public NavigationNamingRules GetNavigationNamingRules() {
        return GetRules().OfType<NavigationNamingRules>().FirstOrDefault();
    }

    protected virtual List<IEdmxRuleModelRoot> GetRules() {
        static LoadRulesResponse Fetch() {
            var applicator = new RuleApplicator(Directory.GetCurrentDirectory());
            var response = applicator.LoadRulesInProjectPath().GetAwaiter().GetResult();
            return response;
        }

        response ??= Fetch();
        return response.Rules;
    }
}