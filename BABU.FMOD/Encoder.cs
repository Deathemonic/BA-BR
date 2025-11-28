#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BABU.FMOD.API;

namespace BABU.FMOD;

public sealed class Encoder : IDisposable
{
    private readonly Lock _lock = new();
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            if (IsInitialized)
            {
                FSBank.FSBank_Release();
                IsInitialized = false;
            }

            _disposed = true;
        }
    }

    public bool Initialize(uint numThreads = 0, string? cacheDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (IsInitialized)
                return true;

            var threads = numThreads == 0 ? (uint)Environment.ProcessorCount : numThreads;

            var result = FSBank.FSBank_Init(
                FSBANK_FSBVERSION.FSB5,
                FSBANK_INITFLAGS.NORMAL,
                threads,
                cacheDirectory);

            if (result != FSBANK_RESULT.OK)
                throw new InvalidOperationException(
                    $"FSBank initialization failed: {FSBankErrors.ErrorString(result)}");

            IsInitialized = true;
            return true;
        }
    }

    public byte[] EncodeToFsb(string inputFile, FSBANK_FORMAT format, uint quality = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        return EncodeToFsb([inputFile], format, quality);
    }

    public byte[] EncodeToFsb(string[] inputFiles, FSBANK_FORMAT format, uint quality = 100)
    {
        ArgumentNullException.ThrowIfNull(inputFiles);
        if (inputFiles.Length == 0)
            throw new ArgumentException("Input files cannot be empty.", nameof(inputFiles));

        EnsureInitialized();

        using var fileNames = new UnmanagedStringArray(inputFiles);

        var subsound = CreateSubsound(fileNames.Pointer, (uint)inputFiles.Length);
        var result = FSBank.FSBank_Build(
            [subsound],
            1,
            format,
            FSBANK_BUILDFLAGS.DEFAULT,
            quality,
            null,
            null);

        if (result != FSBANK_RESULT.OK)
            throw new InvalidOperationException($"FSBank build failed: {FSBankErrors.ErrorString(result)}");

        result = FSBank.FSBank_FetchFSBMemory(out var dataPtr, out var length);
        if (result != FSBANK_RESULT.OK)
            throw new InvalidOperationException($"FSBank fetch memory failed: {FSBankErrors.ErrorString(result)}");

        var fsbData = new byte[length];
        unsafe
        {
            new Span<byte>(dataPtr.ToPointer(), (int)length).CopyTo(fsbData);
        }

        return fsbData;
    }

    public void EncodeToFile(string inputFile, string outputFile, FSBANK_FORMAT format, uint quality = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);
        EncodeToFile([inputFile], outputFile, format, quality);
    }

    public void EncodeToFile(string[] inputFiles, string outputFile, FSBANK_FORMAT format, uint quality = 100)
    {
        ArgumentNullException.ThrowIfNull(inputFiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);
        if (inputFiles.Length == 0)
            throw new ArgumentException("Input files cannot be empty.", nameof(inputFiles));

        EnsureInitialized();

        using var fileNames = new UnmanagedStringArray(inputFiles);

        var subsound = CreateSubsound(fileNames.Pointer, (uint)inputFiles.Length);
        var result = FSBank.FSBank_Build(
            [subsound],
            1,
            format,
            FSBANK_BUILDFLAGS.DEFAULT,
            quality,
            null,
            outputFile);

        if (result != FSBANK_RESULT.OK)
            throw new InvalidOperationException($"FSBank build failed: {FSBankErrors.ErrorString(result)}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsInitialized)
            throw new InvalidOperationException("Encoder not initialized. Call Initialize() first.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FSBANK_SUBSOUND CreateSubsound(nint fileNamesPtr, uint numFiles) => new()
    {
        fileNames = fileNamesPtr,
        fileData = 0,
        fileDataLengths = 0,
        numFiles = numFiles,
        overrideFlags = FSBANK_BUILDFLAGS.DEFAULT,
        overrideQuality = 0,
        desiredSampleRate = 0,
        percentOptimizedRate = 0
    };

    private readonly ref struct UnmanagedStringArray
    {
        private readonly int _count;

        public nint Pointer { get; }

        public UnmanagedStringArray(string[] strings)
        {
            _count = strings.Length;

            unsafe
            {
                Pointer = (nint)NativeMemory.Alloc((nuint)(IntPtr.Size * _count));
                var ptrArray = (nint*)Pointer;

                for (var i = 0; i < _count; i++) ptrArray[i] = Marshal.StringToHGlobalAnsi(strings[i]);
            }
        }

        public void Dispose()
        {
            if (Pointer == 0)
                return;

            unsafe
            {
                var ptrArray = (nint*)Pointer;

                for (var i = 0; i < _count; i++)
                    if (ptrArray[i] != 0)
                        Marshal.FreeHGlobal(ptrArray[i]);

                NativeMemory.Free((void*)Pointer);
            }
        }
    }
}