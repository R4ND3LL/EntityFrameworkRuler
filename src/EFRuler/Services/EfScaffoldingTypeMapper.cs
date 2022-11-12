using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary>
/// EF Type mapper
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfRulerScaffoldingTypeMapper : ScaffoldingTypeMapper, IScaffoldingTypeMapper {
    private readonly IRelationalTypeMappingSource typeMappingSource;

    /// <summary>
    ///
    /// </summary>
    /// <param name="typeMappingSource"></param>
    public EfRulerScaffoldingTypeMapper(IRelationalTypeMappingSource typeMappingSource) : base(typeMappingSource) {
        this.typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="storeType"></param>
    /// <param name="keyOrIndex"></param>
    /// <param name="rowVersion"></param>
    /// <returns></returns>
    public override TypeScaffoldingInfo FindMapping(string storeType, bool keyOrIndex, bool rowVersion) {
        var efDefault = base.FindMapping(storeType, keyOrIndex, rowVersion);
        return efDefault;
    }
}