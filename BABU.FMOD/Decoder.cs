using System.Runtime.InteropServices;
using BABU.FMOD.API;
using FmodSystem = BABU.FMOD.API.System;

namespace BABU.FMOD;

public class Decoder : IDisposable
{
    private readonly Lock _lock = new();
    private FmodSystem _system;
    private bool _initialized;
    private bool _disposed;

    public bool IsInitialized => _initialized;

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            if (_initialized)
            {
                _system.close();
                _system.release();
                _initialized = false;
            }

            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public bool Initialize()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_initialized) return true;

            var result = Factory.System_Create(out _system);
            if (result != RESULT.OK)
                throw new InvalidOperationException($"FMOD System creation failed: {FMODErrors.ErrorString(result)}");

            result = _system.init(32, INITFLAGS.NORMAL, IntPtr.Zero);
            if (result != RESULT.OK)
                throw new InvalidOperationException($"FMOD System initialization failed: {FMODErrors.ErrorString(result)}");

            _initialized = true;
            return true;
        }
    }

    public AudioInfo GetFsbInfo(byte[] fsbData)
    {
        lock (_lock)
        {
            if (!_initialized) Initialize();
        }

        Sound sound = default;
        Sound subsound = default;

        try
        {
            var exInfo = new CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = (uint)fsbData.Length
            };

            var result = _system.createSound(fsbData,
                MODE.OPENMEMORY,
                ref exInfo,
                out sound);

            if (result != RESULT.OK)
                throw new InvalidOperationException($"Failed to load FSB data: {FMODErrors.ErrorString(result)}");

            result = sound.getSubSound(0, out subsound);
            if (result != RESULT.OK)
                throw new InvalidOperationException($"Failed to get subsound: {FMODErrors.ErrorString(result)}");

            subsound.getDefaults(out var frequency, out _);
            subsound.getFormat(out _, out _, out var channels, out _);
            subsound.getLength(out var lengthMs, TIMEUNIT.MS);

            return new AudioInfo
            {
                Frequency = (int)frequency,
                Channels = channels,
                Length = lengthMs / 1000f
            };
        }
        finally
        {
            if (subsound.hasHandle()) subsound.release();
            if (sound.hasHandle()) sound.release();
        }
    }

    public byte[] DecodeToWav(byte[] fsbData)
    {
        lock (_lock)
        {
            if (!_initialized) Initialize();
        }

        Sound sound = default;
        Sound subsound = default;
        IntPtr ptr1 = IntPtr.Zero, ptr2 = IntPtr.Zero;
        uint len1 = 0, len2 = 0;

        try
        {
            var exInfo = new CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = (uint)fsbData.Length
            };

            var result = _system.createSound(fsbData,
                MODE.OPENMEMORY | MODE.CREATESAMPLE | MODE.ACCURATETIME,
                ref exInfo,
                out sound);

            if (result != RESULT.OK)
                throw new InvalidOperationException($"Failed to load FSB data: {FMODErrors.ErrorString(result)}");

            result = sound.getSubSound(0, out subsound);
            if (result != RESULT.OK)
                throw new InvalidOperationException($"Failed to get subsound: {FMODErrors.ErrorString(result)}");

            subsound.getFormat(out _, out var format, out var channels, out var bits);
            subsound.getDefaults(out var frequency, out _);
            subsound.getLength(out var lengthBytes, TIMEUNIT.PCMBYTES);

            result = subsound.@lock(0, lengthBytes, out ptr1, out ptr2, out len1, out len2);
            if (result != RESULT.OK)
                throw new InvalidOperationException($"Failed to lock sound buffer: {FMODErrors.ErrorString(result)}");

            var isFloat = format == SOUND_FORMAT.PCMFLOAT;
            var finalLen = isFloat ? (len1 + len2) / 2 : (len1 + len2);
            var finalBits = isFloat ? 16 : bits;

            var wavData = new byte[44 + finalLen];
            WriteWavHeader(wavData, (int)finalLen, (int)frequency, channels, finalBits);

            var offset = 44;
            if (isFloat)
            {
                if (ptr1 != IntPtr.Zero && len1 > 0)
                    offset += ConvertFloatToPcm16(ptr1, len1, wavData.AsSpan(offset));
                if (ptr2 != IntPtr.Zero && len2 > 0)
                    ConvertFloatToPcm16(ptr2, len2, wavData.AsSpan(offset));
            }
            else
            {
                if (ptr1 != IntPtr.Zero && len1 > 0)
                {
                    Marshal.Copy(ptr1, wavData, offset, (int)len1);
                    offset += (int)len1;
                }
                if (ptr2 != IntPtr.Zero && len2 > 0)
                    Marshal.Copy(ptr2, wavData, offset, (int)len2);
            }

            return wavData;
        }
        finally
        {
            if (ptr1 != IntPtr.Zero || ptr2 != IntPtr.Zero)
                subsound.unlock(ptr1, ptr2, len1, len2);

            if (subsound.hasHandle()) subsound.release();
            if (sound.hasHandle()) sound.release();
        }
    }

    private static int ConvertFloatToPcm16(IntPtr srcPtr, uint srcLen, Span<byte> dest)
    {
        var floatCount = (int)(srcLen / 4);
        var destShorts = MemoryMarshal.Cast<byte, short>(dest);

        unsafe
        {
            var floatPtr = (float*)srcPtr;
            for (var i = 0; i < floatCount; i++)
            {
                var sample = floatPtr[i];
                if (sample > 1.0f) sample = 1.0f;
                else if (sample < -1.0f) sample = -1.0f;

                destShorts[i] = (short)(sample * 32767f);
            }
        }
        return floatCount * 2;
    }

    private static void WriteWavHeader(Span<byte> buffer, int pcmDataLength, int sampleRate, int channels, int bitsPerSample)
    {
        var header = new WavHeader
        {
            ChunkId = 0x46464952,
            ChunkSize = (uint)(36 + pcmDataLength),
            Format = 0x45564157,
            Subchunk1Id = 0x20746d66,
            Subchunk1Size = 16,
            AudioFormat = 1,
            NumChannels = (ushort)channels,
            SampleRate = (uint)sampleRate,
            ByteRate = (uint)(sampleRate * channels * bitsPerSample / 8),
            BlockAlign = (ushort)(channels * bitsPerSample / 8),
            BitsPerSample = (ushort)bitsPerSample,
            Subchunk2Id = 0x61746164,
            Subchunk2Size = (uint)pcmDataLength
        };
        MemoryMarshal.Write(buffer, in header);
    }

    public readonly struct AudioInfo
    {
        public int Frequency { get; init; }
        public int Channels { get; init; }
        public float Length { get; init; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct WavHeader
    {
        public uint ChunkId;
        public uint ChunkSize;
        public uint Format;
        public uint Subchunk1Id;
        public uint Subchunk1Size;
        public ushort AudioFormat;
        public ushort NumChannels;
        public uint SampleRate;
        public uint ByteRate;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public uint Subchunk2Id;
        public uint Subchunk2Size;
    }
}