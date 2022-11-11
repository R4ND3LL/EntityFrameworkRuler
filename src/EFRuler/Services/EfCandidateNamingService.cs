using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary> Naming service override to be used by Ef scaffold process. </summary>
public class EfCandidateNamingService : CandidateNamingService {
    public override string GenerateCandidateIdentifier(DatabaseTable originalTable) {
        if (!Debugger.IsAttached) Debugger.Launch();
        return base.GenerateCandidateIdentifier(originalTable);
    }

    public override string GenerateCandidateIdentifier(DatabaseColumn originalColumn) {
        return base.GenerateCandidateIdentifier(originalColumn);
    }

    public override string GetDependentEndCandidateNavigationPropertyName(IForeignKey foreignKey) {
        return base.GetDependentEndCandidateNavigationPropertyName(foreignKey);
    }

    public override string GetPrincipalEndCandidateNavigationPropertyName(IForeignKey foreignKey,
        string dependentEndNavigationPropertyName) {
        return base.GetPrincipalEndCandidateNavigationPropertyName(foreignKey, dependentEndNavigationPropertyName);
    }
}