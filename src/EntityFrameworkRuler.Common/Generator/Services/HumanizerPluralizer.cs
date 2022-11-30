// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable MemberCanBeInternal

using Humanizer;

namespace EntityFrameworkRuler.Generator.Services;

/// <summary>
/// This is the default IPluralizer implementation used by EF.
/// Borrowed from Microsoft.EntityFrameworkCore.Design.Internal.HumanizerPluralizer
/// </summary>
public class HumanizerPluralizer : IRulerPluralizer {
    /// <inheritdoc />
    public virtual string Pluralize(string name) => name.Pluralize(inputIsKnownToBeSingular: false);
    /// <inheritdoc />
    public virtual string Singularize(string name) => name.Singularize(inputIsKnownToBePlural: false);
}