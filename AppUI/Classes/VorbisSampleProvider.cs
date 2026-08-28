using System;
using System.IO;
using NAudio.Wave;
using NVorbis;

namespace AppUI.Classes
{
    public sealed class VorbisSampleProvider : ISampleProvider, IDisposable
    {
        private readonly VorbisReader _reader;

        public VorbisSampleProvider(string path)
        {
            _reader = new VorbisReader(path);
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
        }

        public VorbisSampleProvider(Stream stream)
        {
            _reader = new VorbisReader(stream, true);
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<float> buffer)
        {
            return _reader.ReadSamples(buffer);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
