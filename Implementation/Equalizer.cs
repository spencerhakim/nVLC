using LibVlcWrapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Implementation
{
    /// <summary>
    /// 
    /// </summary>
    public class Equalizer : DisposableBase
    {
        private IntPtr _handle;
        private ReadOnlyCollection<Band> _bands;

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<Preset> Presets
        {
            get
            {
                int count = LibVlcMethods.libvlc_audio_equalizer_get_preset_count();
                for (int i = 0; i < count; i++)
                {
                    yield return new Preset(i, Marshal.PtrToStringAnsi(LibVlcMethods.libvlc_audio_equalizer_get_preset_name(i)));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Equalizer()
        {
            _handle = LibVlcMethods.libvlc_audio_equalizer_new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="preset"></param>
        public Equalizer(Preset preset)
        {
            _handle = LibVlcMethods.libvlc_audio_equalizer_new_from_preset(preset.Index);
        }

        /// <summary>
        /// 
        /// </summary>
        public float Preamp
        {
            get 
            {
                return LibVlcMethods.libvlc_audio_equalizer_get_preamp(_handle); 
            }
            set 
            {
                LibVlcMethods.libvlc_audio_equalizer_set_preamp(_handle, value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            LibVlcMethods.libvlc_audio_equalizer_release(_handle);
        }

        /// <summary>
        /// 
        /// </summary>
        public ReadOnlyCollection<Band> Bands
        {
            get
            {
                if (_bands == null)
                {
                    int count = LibVlcMethods.libvlc_audio_equalizer_get_band_count();
                    List<Band> temp = new List<Band>(count);
                    for (int i = 0; i < count; i++)
                    {
                        temp.Add(new Band(i, LibVlcMethods.libvlc_audio_equalizer_get_band_frequency(i), _handle));
                    }

                    _bands = new ReadOnlyCollection<Band>(temp);
                }

                return _bands;
            }
        }

        internal IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public sealed class Preset
    {
        internal Preset(int index, string name)
        {
            Index = index;
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Band
    {
        private IntPtr _handle;

        internal Band(int index, float frequency, IntPtr hEqualizer)
        {
            Index = index;
            Frequency = frequency;
            _handle = hEqualizer;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public float Frequency { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public float Amplitude 
        { 
            get
            {
                return LibVlcMethods.libvlc_audio_equalizer_get_amp_at_index(_handle, Index);
            }
            set
            {
                LibVlcMethods.libvlc_audio_equalizer_set_amp_at_index(_handle, value, Index);
            }
        }

        public override string ToString()
        {
            return string.Format("Frequency : {0}, Amplitude : {1}", Frequency, Amplitude);
        }
    }
}
