using System.Runtime.InteropServices;
using QuickPlay.Audio;

namespace QuickPlay.Waveform;

public sealed partial class BassWaveformAnalyzer : IWaveformAnalyzer
{
    public Task<WaveformData> AnalyzeAsync(string filePath, int peakCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentOutOfRangeException.ThrowIfLessThan(peakCount, 1);
        return Task.Run(() => Analyze(filePath, peakCount, cancellationToken), cancellationToken);
    }

    private static WaveformData Analyze(string filePath, int peakCount, CancellationToken cancellationToken)
    {
        const uint bassUnicode = 0x80000000;
        const uint bassStreamDecode = 0x200000;
        const uint bassDataFloat = 0x40000000;
        var stream = BassNative.BASS_StreamCreateFile(false, BassFilePath.Prepare(filePath), 0, 0, bassUnicode | bassStreamDecode);
        if (stream == nint.Zero) return new WaveformData([]);

        try
        {
            var length = BassNative.BASS_ChannelGetLength(stream, 0);
            var durationSeconds = BassNative.BASS_ChannelBytes2Seconds(stream, length);
            if (durationSeconds <= 0) return new WaveformData([]);

            // Sample a small decoded window at evenly spaced positions. This keeps
            // analysis fast and bounded even for multi-hundred-megabyte AIFF files.
            var peaks = new float[peakCount];
            var buffer = new float[8192];
            for (var peakIndex = 0; peakIndex < peakCount; peakIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var seconds = durationSeconds * peakIndex / peakCount;
                var position = BassNative.BASS_ChannelSeconds2Bytes(stream, seconds);
                if (!BassNative.BASS_ChannelSetPosition(stream, position, 0)) continue;
                var byteCount = BassNative.BASS_ChannelGetData(stream, buffer, (uint)(buffer.Length * sizeof(float)) | bassDataFloat);
                if (byteCount <= 0) continue;
                var sampleCount = byteCount / sizeof(float);
                for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    peaks[peakIndex] = Math.Max(peaks[peakIndex], Math.Abs(buffer[sampleIndex]));
            }
            return new WaveformData(peaks);
        }
        finally
        {
            BassNative.BASS_StreamFree(stream);
        }
    }

    private static partial class BassNative
    {
        [LibraryImport("bass.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint BASS_StreamCreateFile([MarshalAs(UnmanagedType.Bool)] bool memory, string file, ulong offset, ulong length, uint flags);
        [LibraryImport("bass.dll")]
        internal static partial int BASS_ChannelGetData(nint handle, [Out] float[] buffer, uint length);
        [LibraryImport("bass.dll")]
        internal static partial ulong BASS_ChannelGetLength(nint handle, uint mode);
        [LibraryImport("bass.dll")]
        internal static partial double BASS_ChannelBytes2Seconds(nint handle, ulong position);
        [LibraryImport("bass.dll")]
        internal static partial ulong BASS_ChannelSeconds2Bytes(nint handle, double position);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_ChannelSetPosition(nint handle, ulong position, uint mode);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_StreamFree(nint handle);
    }
}
