using BABU.Services.Bundle;

namespace BABU.Models.Context;

public readonly record struct ComparisonContext(
    BundleLoaderService ModdedLoaderService,
    BundleLoaderService PatchLoaderService,
    ProcessingOptions Options);