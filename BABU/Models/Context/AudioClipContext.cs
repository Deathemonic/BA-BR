using BABU.FMOD.API;

namespace BABU.Models.Context;

public readonly record struct AudioFileInfo(string FilePath, FSBANK_FORMAT Format);