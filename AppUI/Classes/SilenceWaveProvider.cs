using NAudio.Wave;
using System;

namespace AppUI.Classes
{
    /// <summary>
    /// A wave provider that plays no sound (used to test audio channels)
    /// </summary>
    public class SilenceWaveProvider : IWaveProvider
    {
        private WaveFormat _waveFormat;

        public SilenceWaveProvider(WaveFormat waveFormat)
        {
            this._waveFormat = waveFormat;
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(Span<byte> buffer)
        {
            // the silenced wave provider will return 0 which indicates playback stopped 
            // ... because PlaybackStopped will be fired once ALL input audio sources are done streaming [which is indicated when Read() returns 0]
            return 0; 
        }
    }
}
