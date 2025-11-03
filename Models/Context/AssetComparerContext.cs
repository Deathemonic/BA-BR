using BABU.Handlers.Bundle;

namespace BABU.Models.Context;

public readonly record struct ComparisonContext
{
    public required BundleLoader ModdedLoader { get; init; }
    public required BundleLoader PatchLoader { get; init; }
    public required ProcessingOptions Options { get; init; }
}

