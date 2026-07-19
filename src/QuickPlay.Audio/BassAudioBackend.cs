using System.ComponentModel;
using System.Runtime.InteropServices;

namespace QuickPlay.Audio;

public sealed partial class BassAudioBackend : IAudioBackend
{
    private const uint BassUnicode = 0x80000000;
    private nint _stream;
    private bool _disposed;

    public BassAudioBackend()
    {
        if (!BassNative.BASS_Init(-1, 44_100, 0, nint.Zero, nint.Zero))
            throw BassError("initialize the audio device");
    }

    public TimeSpan Position
    {
        get
        {
            EnsureStream();
            var position = BassNative.BASS_ChannelGetPosition(_stream, 0);
            return TimeSpan.FromSeconds(Math.Max(0, BassNative.BASS_ChannelBytes2Seconds(_stream, position)));
        }
    }

    public bool IsPlaying => _stream != nint.Zero && BassNative.BASS_ChannelIsActive(_stream) == 1;

    public TimeSpan Open(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        FreeStream();
        _stream = BassNative.BASS_StreamCreateFile(false, filePath, 0, 0, BassUnicode);
        if (_stream == nint.Zero) throw BassError($"open '{filePath}'");
        var length = BassNative.BASS_ChannelGetLength(_stream, 0);
        var seconds = BassNative.BASS_ChannelBytes2Seconds(_stream, length);
        return TimeSpan.FromSeconds(Math.Max(0, seconds));
    }

    public void Seek(TimeSpan position)
    {
        EnsureStream();
        var bytePosition = BassNative.BASS_ChannelSeconds2Bytes(_stream, position.TotalSeconds);
        if (!BassNative.BASS_ChannelSetPosition(_stream, bytePosition, 0)) throw BassError("seek");
    }

    public void Play()
    {
        EnsureStream();
        if (!BassNative.BASS_ChannelPlay(_stream, false)) throw BassError("start playback");
    }

    public void Pause()
    {
        EnsureStream();
        if (!BassNative.BASS_ChannelPause(_stream)) throw BassError("pause playback");
    }

    public void Stop()
    {
        if (_stream != nint.Zero) BassNative.BASS_ChannelStop(_stream);
    }

    public void Close()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();
        FreeStream();
    }

    public void Dispose()
    {
        if (_disposed) return;
        FreeStream();
        BassNative.BASS_Free();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void EnsureStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_stream == nint.Zero) throw new InvalidOperationException("No track is loaded.");
    }

    private void FreeStream()
    {
        if (_stream == nint.Zero) return;
        BassNative.BASS_StreamFree(_stream);
        _stream = nint.Zero;
    }

    private static Win32Exception BassError(string operation) =>
        new(BassNative.BASS_ErrorGetCode(), $"BASS could not {operation}.");

    private static partial class BassNative
    {
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_Init(int device, uint frequency, uint flags, nint window, nint dsguid);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_Free();
        [LibraryImport("bass.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint BASS_StreamCreateFile([MarshalAs(UnmanagedType.Bool)] bool memory, string file, ulong offset, ulong length, uint flags);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_StreamFree(nint handle);
        [LibraryImport("bass.dll")]
        internal static partial ulong BASS_ChannelGetLength(nint handle, uint mode);
        [LibraryImport("bass.dll")]
        internal static partial ulong BASS_ChannelGetPosition(nint handle, uint mode);
        [LibraryImport("bass.dll")]
        internal static partial double BASS_ChannelBytes2Seconds(nint handle, ulong position);
        [LibraryImport("bass.dll")]
        internal static partial ulong BASS_ChannelSeconds2Bytes(nint handle, double position);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_ChannelSetPosition(nint handle, ulong position, uint mode);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_ChannelPlay(nint handle, [MarshalAs(UnmanagedType.Bool)] bool restart);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_ChannelPause(nint handle);
        [LibraryImport("bass.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BASS_ChannelStop(nint handle);
        [LibraryImport("bass.dll")]
        internal static partial uint BASS_ChannelIsActive(nint handle);
        [LibraryImport("bass.dll")]
        internal static partial int BASS_ErrorGetCode();
    }
}
