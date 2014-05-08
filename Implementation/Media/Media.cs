//    nVLC
//    
//    Author:  Roman Ginzburg
//
//    nVLC is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    nVLC is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.
//     
// ========================================================================

using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Structures;
using Implementation.Events;
using Implementation.Exceptions;
using Implementation.Utils;
using LibVlcWrapper;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Implementation.Media
{
    internal class BasicMedia : DisposableBase, IMedia, INativePointer, IReferenceCount, IEventProvider
    {
        protected readonly IntPtr m_hMediaLib;
        protected IntPtr m_hMedia;
        protected string m_path;
        IntPtr m_hEventManager = IntPtr.Zero;
        IMediaEvents m_events;
        private SlaveMedia _slaveMedia;

        public BasicMedia(IntPtr hMediaLib)
        {
            m_hMediaLib = hMediaLib;
        }

        public BasicMedia(IntPtr hMedia, ReferenceCountAction refCountAction)
        {
            m_hMedia = hMedia;
            IntPtr pData = LibVlcMethods.libvlc_media_get_mrl(m_hMedia);
            m_path = Marshal.PtrToStringAnsi(pData);
            switch (refCountAction)
            {
                case ReferenceCountAction.AddRef:
                    AddRef();
                    break;

                case ReferenceCountAction.Release:
                    Release();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Release();
        }

        #region IMedia Members

        public virtual string Input
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value;
                m_hMedia = LibVlcMethods.libvlc_media_new_location(m_hMediaLib, m_path.ToUtf8());
            }
        }

        public MediaState State
        {
            get
            {
                return (MediaState)LibVlcMethods.libvlc_media_get_state(m_hMedia);
            }
        }

        public void AddOptions(IEnumerable<string> options)
        {
            foreach (var item in options)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    LibVlcMethods.libvlc_media_add_option(m_hMedia, item.ToUtf8());
                }
            }
        }

        public void AddOptionFlag(string option, int flag)
        {
            LibVlcMethods.libvlc_media_add_option_flag(m_hMedia, option.ToUtf8(), flag);
        }

        public IMedia Duplicate()
        {
            IntPtr clone = LibVlcMethods.libvlc_media_duplicate(m_hMedia);
            return new BasicMedia(clone, ReferenceCountAction.None);
        }

        public void Parse(bool aSync)
        {
            if (aSync)
            {
                LibVlcMethods.libvlc_media_parse_async(m_hMedia);
            }
            else
            {
                LibVlcMethods.libvlc_media_parse(m_hMedia);
            }
        }

        public bool IsParsed
        {
            get
            {
                return LibVlcMethods.libvlc_media_is_parsed(m_hMedia);
            }
        }

        public IntPtr Tag
        {
            get
            {
                return LibVlcMethods.libvlc_media_get_user_data(m_hMedia);
            }
            set
            {
                LibVlcMethods.libvlc_media_set_user_data(m_hMedia, value);
            }
        }

        public IMediaEvents Events
        {
            get
            {
                if (m_events == null)
                {
                    m_events = new MediaEventManager(this);
                }

                return m_events;
            }
        }

        public MediaStatistics Statistics
        {
            get
            {
                libvlc_media_stats_t t;

                int num = LibVlcMethods.libvlc_media_get_stats(m_hMedia, out t);

                return t.ToMediaStatistics();
            }
        }

        public IMediaList SubItems
        {
            get
            {
                IntPtr hMediaList = LibVlcMethods.libvlc_media_subitems(m_hMedia);
                if (hMediaList == IntPtr.Zero)
                {
                    return null;
                }

                return new MediaList(hMediaList, ReferenceCountAction.None);
            }
        }

        public MediaTrack[] TracksInfoEx
        {
            get
            {
                unsafe
                {
                    libvlc_media_track_t** ppTracks;
                    int num = LibVlcMethods.libvlc_media_tracks_get(m_hMedia, &ppTracks);
                    if (num == 0 || ppTracks == null)
                    {
                        throw new LibVlcException();
                    }

                    List<MediaTrack> list = new List<MediaTrack>(num);
                    for (int i = 0; i < num; i++)
                    {
                        MediaTrack track = null;
                        libvlc_media_track_t* pTrackInfo = ppTracks[i];
                        switch (pTrackInfo->i_type)
                        {
                            case libvlc_track_type_t.libvlc_track_audio:
                                AudioTrack audio = new AudioTrack();
                                libvlc_audio_track_t* audioTrack = (libvlc_audio_track_t*)pTrackInfo->media.ToPointer();
                                audio.Channels = audioTrack->i_channels;
                                audio.Rate = audioTrack->i_rate;
                                track = audio;
                                break;

                            case libvlc_track_type_t.libvlc_track_video:
                                VideoTrack video = new VideoTrack();
                                libvlc_video_track_t* videoTrack = (libvlc_video_track_t*)pTrackInfo->media.ToPointer();
                                video.Width = videoTrack->i_width;
                                video.Height = videoTrack->i_height;
                                video.Sar_den = videoTrack->i_sar_den;
                                video.Sar_num = videoTrack->i_sar_num;
                                video.Frame_rate_den = videoTrack->i_frame_rate_den;
                                video.Frame_rate_num = videoTrack->i_frame_rate_num;
                                track = video;
                                break;

                            case libvlc_track_type_t.libvlc_track_text:
                                SubtitlesTrack sub = new SubtitlesTrack();
                                libvlc_subtitle_track_t* subtitleTrack = (libvlc_subtitle_track_t*)pTrackInfo->media.ToPointer();
                                sub.Encoding = subtitleTrack->psz_encoding == null ? null : Marshal.PtrToStringAnsi(subtitleTrack->psz_encoding);
                                track = sub;
                                break;

                            case libvlc_track_type_t.libvlc_track_unknown:
                            default:
                                track = new MediaTrack();
                                break;
                        }

                        track.Bitrate = pTrackInfo->i_bitrate;
                        track.Codec = MiscUtils.DwordToFourCC(pTrackInfo->i_codec);
                        track.Id = pTrackInfo->i_id;
                        track.OriginalFourCC = MiscUtils.DwordToFourCC(pTrackInfo->i_original_fourcc);
                        track.Language = pTrackInfo->psz_language == null ? null : Marshal.PtrToStringAnsi(pTrackInfo->psz_language);
                        track.Description = pTrackInfo->psz_description == null ? null : Marshal.PtrToStringAnsi(pTrackInfo->psz_description);
                        list.Add(track);
                    }

                    LibVlcMethods.libvlc_media_tracks_release(ppTracks, num);
                    return list.ToArray();
                }
            }
        }

        #endregion

        #region INativePointer Members

        public IntPtr Pointer
        {
            get
            {
                return m_hMedia;
            }
        }

        #endregion

        #region IReferenceCount Members

        public void AddRef()
        {
            LibVlcMethods.libvlc_media_retain(m_hMedia);
        }

        public void Release()
        {
            try
            {
                LibVlcMethods.libvlc_media_release(m_hMedia);
            }
            catch (Exception)
            { }
        }

        #endregion

        #region IEventProvider Members

        public IntPtr EventManagerHandle
        {
            get
            {
                if (m_hEventManager == IntPtr.Zero)
                {
                    m_hEventManager = LibVlcMethods.libvlc_media_event_manager(m_hMedia);
                }

                return m_hEventManager;
            }
        }

        #endregion

        #region IEqualityComparer<IMedia> Members

        public bool Equals(IMedia x, IMedia y)
        {
            INativePointer x1 = (INativePointer)x;
            INativePointer y1 = (INativePointer)y;

            return x1.Pointer == y1.Pointer;
        }

        public int GetHashCode(IMedia obj)
        {
            return ((INativePointer)obj).Pointer.GetHashCode();
        }

        #endregion

        public override bool Equals(object obj)
        {
            return this.Equals((IMedia)obj, this);
        }

        public override int GetHashCode()
        {
            return m_hMedia.GetHashCode();
        }

        public SlaveMedia SlaveMedia
        {
            get
            {
                return _slaveMedia;
            }
            set
            {
                AddOptions(new[] { value.ToString() });
                _slaveMedia = value;
            }
        }
    }
}
