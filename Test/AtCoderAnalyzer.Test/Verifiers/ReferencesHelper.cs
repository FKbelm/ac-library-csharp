﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace AtCoderAnalyzer.Test
{
    internal static class ReferencesHelper
    {
        internal static ImmutableArray<PackageIdentity> Packages
            = ImmutableArray.Create(new PackageIdentity("ac-library-csharp", "1.0.3-beta.1"));
    }
}
