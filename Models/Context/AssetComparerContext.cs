using BABU.Services.Bundle;

namespace BABU.Models.Context;

public readonly record struct ComparisonContext
{
    public required BundleLoaderService ModdedLoaderService { get; init; }
    public required BundleLoaderService PatchLoaderService { get; init; }
    public required ProcessingOptions Options { get; init; }
}

