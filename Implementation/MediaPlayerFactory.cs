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
using Declarations.Discovery;
using Declarations.Media;
using Declarations.MediaLibrary;
using Declarations.Players;
using Declarations.VLM;
using Implementation.Discovery;
using Implementation.Exceptions;
using Implementation.Loggers;
using Implementation.Media;
using Implementation.MediaLibrary;
using Implementation.Players;
using Implementation.Utils;
using Implementation.VLM;
using LibVlcWrapper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Implementation
{
    /// <summary>
    /// Entry point for the nVLC library.
    /// </summary>
    public class MediaPlayerFactory : DisposableBase, IMediaPlayerFactory, IReferenceCount, INativePointer
    {
        IntPtr m_hMediaLib = IntPtr.Zero;
        IVideoLanManager m_vlm = null;
        NLogger m_logger = new NLogger();
        LogSubscriber m_log;
        string m_currentDir;

        /// <summary>
        /// Initializes media library with default arguments
        /// </summary>
        /// <param name="findLibvlc"></param>
        /// <param name="useCustomStringMarshaller"></param>
        /// <remarks>Default arguments:
        /// "-I",
        /// "dumy",  
		/// "--ignore-config", 
        /// "--no-osd",
        /// "--disable-screensaver",
        /// "--plugin-path=./plugins"
        /// </remarks>
        public MediaPlayerFactory(bool findLibvlc = false, bool useCustomStringMarshaller = false)
        {
            var args = new string[]
            {
                "-I", 
                "dumy",  
		        "--ignore-config", 
                "--no-osd",
                "--disable-screensaver",
		        "--plugin-path=./plugins" 
            };

            Initialize(args, findLibvlc, useCustomStringMarshaller);
        }

        /// <summary>
        /// Initializes media library with user defined arguments
        /// </summary>
        /// <param name="args">Collection of arguments passed to libVLC library</param>
        /// <param name="findLibvlc">True to find libvlc installation path, False to use libvlc in the executable path</param>
        /// <param name="useCustomStringMarshaller"></param>
        public MediaPlayerFactory(string[] args, bool findLibvlc = false, bool useCustomStringMarshaller = false)
        {
            Initialize(args, findLibvlc, useCustomStringMarshaller);
        }

        private void Initialize(string[] args, bool findLibvlc, bool useCustomStringMarshaller)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            if (findLibvlc)
            {
                TrySetVLCPath();
            }

            try
            {
                if (useCustomStringMarshaller)
                    m_hMediaLib = LibVlcMethods.libvlc_new_custom_marshaller(args.Length, args);
                else
                    m_hMediaLib = LibVlcMethods.libvlc_new(args.Length, args);
            }
            catch (DllNotFoundException ex)
            {
                throw new LibVlcNotFoundException(ex);
            }

            if (m_hMediaLib == IntPtr.Zero)
            {
                throw new LibVlcInitException();
            }

            if (findLibvlc)
            {
                Directory.SetCurrentDirectory(m_currentDir);
            }

            TrySetupLogging();
            TryFilterRemovedModules();            
        }

        private void TryFilterRemovedModules()
        {
            try
            {
                const string pattern = @"\d+(\.\d+)+"; // numbers separated by dots
                Match versionMatch = Regex.Match(Version, pattern);
                Version libVlcVersion = new Version(versionMatch.Value);
                ObjectFactory.FilterRemovedModules(libVlcVersion);
            }
            catch (Exception)
            { }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exc = e.ExceptionObject as Exception;
            if (exc != null)
            {
                var error = MiscUtils.FindNestedException<EntryPointNotFoundException>(exc);
                if (error != null)
                {
                    string ver = MiscUtils.GetMinimalSupportedVersion(error);
                    m_logger.Error(string.Format("Method {0} supported starting libVLC version {1}", error.TargetSite.Name, ver));
                }
                else
                {
                    string msg = MiscUtils.LogNestedException(exc);
                    m_logger.Error("Unhandled exception: " + msg);
                }
            }

            if (e.IsTerminating)
            {
                m_logger.Error("Due to unhandled exception the application will terminate");
            }
        }

        private void TrySetupLogging()
        {
            try
            {
                m_log = new LogSubscriber(m_logger, m_hMediaLib);
            }
            catch (EntryPointNotFoundException ex)
            {
                string name = ex.TargetSite.Name;
                string minVersion = MiscUtils.GetMinimalSupportedVersion(ex);
                if (!string.IsNullOrEmpty(minVersion))
                {
                    string msg = string.Format("libVLC logging functinality enabled staring libVLC version {0} while you are using version {1}", minVersion, Version);
                    m_logger.Warning(msg);
                }
                else
                {
                    m_logger.Warning(ex.Message);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Failed to setup logging, reason : {0}", ex.Message);
                m_logger.Error(msg);
            }
        }

        /// <summary>
        /// Creates new instance of player.
        /// </summary>
        /// <typeparam name="T">Type of the player to create</typeparam>
        /// <returns>Newly created player</returns>
        public T CreatePlayer<T>() where T : IPlayer
        {
            return ObjectFactory.Build<T>(m_hMediaLib);
        }

        /// <summary>
        /// Creates new instance of media list player
        /// </summary>
        /// <typeparam name="T">Type of media list player</typeparam>
        /// <param name="mediaList">Media list</param>
        /// <returns>Newly created media list player</returns>
        public T CreateMediaListPlayer<T>(IMediaList mediaList) where T : IMediaListPlayer
        {
            return ObjectFactory.Build<T>(m_hMediaLib, mediaList);
        }

        /// <summary>
        /// Creates new instance of media.
        /// </summary>
        /// <typeparam name="T">Type of media to create</typeparam>
        /// <param name="input">The media input string</param>
        /// <param name="options">Optional media options</param>
        /// <returns>Newly created media</returns>
        public T CreateMedia<T>(string input, params string[] options) where T : IMedia
        {
            T media = ObjectFactory.Build<T>(m_hMediaLib);
            media.Input = input;
            media.AddOptions(options);

            return media;
        }

        /// <summary>
        /// Creates new instance of media list.
        /// </summary>
        /// <typeparam name="T">Type of media list</typeparam>
        /// <param name="mediaItems">Collection of media inputs</param>       
        /// <param name="options"></param>
        /// <returns>Newly created media list</returns>
        public T CreateMediaList<T>(IEnumerable<string> mediaItems, params string[] options) where T : IMediaList
        {
            T mediaList = ObjectFactory.Build<T>(m_hMediaLib);
            foreach (var file in mediaItems)
            {
                mediaList.Add(this.CreateMedia<IMedia>(file, options));
            }

            return mediaList;
        }

        /// <summary>
        /// Creates media list instance with no media items
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CreateMediaList<T>() where T : IMediaList
        {
            return ObjectFactory.Build<T>(m_hMediaLib);
        }

        /// <summary>
        /// Creates media discovery object
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IMediaDiscoverer CreateMediaDiscoverer(string name)
        {
            return ObjectFactory.Build<IMediaDiscoverer>(m_hMediaLib, name);
        }

        /// <summary>
        /// Creates media library
        /// </summary>
        /// <returns></returns>
        public IMediaLibrary CreateMediaLibrary()
        {
            return ObjectFactory.Build<IMediaLibrary>(m_hMediaLib);
        }

        /// <summary>
        /// Gets the libVLC version.
        /// </summary>
        public string Version
        {
            get
            {
                IntPtr pStr = LibVlcMethods.libvlc_get_version();
                return Marshal.PtrToStringAnsi(pStr);
            }               
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Release();
        }

        private static class ObjectFactory
        {
            static Dictionary<Type, Type> objectMap = new Dictionary<Type, Type>();
            static Dictionary<Type, LibVlcRemovedModuleException> removedModules = new Dictionary<Type, LibVlcRemovedModuleException>();
            
            static ObjectFactory()
            {
                objectMap.Add(typeof(IMedia), typeof(BasicMedia));
                objectMap.Add(typeof(IMediaFromFile), typeof(MediaFromFile));
                objectMap.Add(typeof(IVideoInputMedia), typeof(VideoInputMedia));
                objectMap.Add(typeof(IScreenCaptureMedia), typeof(ScreenCaptureMedia));
                objectMap.Add(typeof(IPlayer), typeof(BasicPlayer));
                objectMap.Add(typeof(IAudioPlayer), typeof(AudioPlayer));
                objectMap.Add(typeof(IVideoPlayer), typeof(VideoPlayer));
                objectMap.Add(typeof(IDiskPlayer), typeof(DiskPlayer));
                objectMap.Add(typeof(IMediaList), typeof(MediaList));
                objectMap.Add(typeof(IMediaListPlayer), typeof(MediaListPlayer));
                objectMap.Add(typeof(IVideoLanManager), typeof(VideoLanManager));
                objectMap.Add(typeof(IMediaDiscoverer), typeof(MediaDiscoverer));
                objectMap.Add(typeof(IMediaLibrary), typeof(MediaLibraryImpl));
                objectMap.Add(typeof(IMemoryInputMedia), typeof(MemoryInputMedia));
                objectMap.Add(typeof(ICompositeMemoryInputMedia), typeof(CompositeMemoryInputMedia));
            }

            public static T Build<T>(params object[] args)
            {
                Type t = typeof(T);
                if (removedModules.ContainsKey(t))
                {
                    throw removedModules[t];
                }
                if (!objectMap.ContainsKey(t))
                {
                    throw new ArgumentException("Unregistered type", t.FullName);                  
                }

                return (T)Activator.CreateInstance(objectMap[t], args);
            }

            public static void FilterRemovedModules(Version currentVersion)
            {
                foreach (var item in objectMap)
                {
                    MaxLibVlcVersion maxVer = (MaxLibVlcVersion)Attribute.GetCustomAttribute(item.Value, typeof(MaxLibVlcVersion));
                    if (maxVer == null)
                    {
                        continue;
                    }
                    
                    Version lastSupported = new Version(maxVer.MaxVersion);
                    if (currentVersion > lastSupported)
                    {
                        removedModules[item.Key] = new LibVlcRemovedModuleException(maxVer.LibVlcModuleName, item.Key.Name, maxVer.MaxVersion);
                    }
                }
            }
        }

        #region IReferenceCount Members

        public void AddRef()
        {
            LibVlcMethods.libvlc_retain(m_hMediaLib);
        }

        public void Release()
        {
            try
            {
                LibVlcMethods.libvlc_release(m_hMediaLib);
            }
            catch (AccessViolationException)
            { }
        }

        #endregion

        #region INativePointer Members

        public IntPtr Pointer
        {
            get
            {
                return m_hMediaLib;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public long Clock
        {
            get
            {
                return LibVlcMethods.libvlc_clock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public long Delay(long pts)
        {
            return LibVlcMethods.libvlc_delay(pts);
        }

        /// <summary>
        /// Gets list of available audio filters
        /// </summary>
        public IEnumerable<FilterInfo> AudioFilters
        {
            get
            {
                IntPtr pList = LibVlcMethods.libvlc_audio_filter_list_get(m_hMediaLib);
                libvlc_module_description_t item = (libvlc_module_description_t)Marshal.PtrToStructure(pList, typeof(libvlc_module_description_t));

                do
                {
                    yield return GetFilterInfo(item);
                    if (item.p_next != IntPtr.Zero)
                    {
                        item = (libvlc_module_description_t)Marshal.PtrToStructure(item.p_next, typeof(libvlc_module_description_t));
                    }
                    else
                    {
                        break;
                    }

                }
                while (true);

                LibVlcMethods.libvlc_module_description_list_release(pList);
            }
        }

        /// <summary>
        /// Gets list of available video filters
        /// </summary>
        public IEnumerable<FilterInfo> VideoFilters
        {
            get
            {
                IntPtr pList = LibVlcMethods.libvlc_video_filter_list_get(m_hMediaLib);
                if (pList == IntPtr.Zero)
                {
                    yield break;
                }

                libvlc_module_description_t item = (libvlc_module_description_t)Marshal.PtrToStructure(pList, typeof(libvlc_module_description_t));

                do
                {
                    yield return GetFilterInfo(item);
                    if (item.p_next != IntPtr.Zero)
                    {
                        item = (libvlc_module_description_t)Marshal.PtrToStructure(item.p_next, typeof(libvlc_module_description_t));
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);

                LibVlcMethods.libvlc_module_description_list_release(pList);
            }
        }

        private FilterInfo GetFilterInfo(libvlc_module_description_t item)
        {
            return new FilterInfo()
            {
                Help = Marshal.PtrToStringAnsi(item.psz_help),
                Longname = Marshal.PtrToStringAnsi(item.psz_longname),
                Name = Marshal.PtrToStringAnsi(item.psz_name),
                Shortname = Marshal.PtrToStringAnsi(item.psz_shortname)
            };
        }

        /// <summary>
        /// Gets the VLM instance
        /// </summary>
        public IVideoLanManager VideoLanManager
        {
            get
            {
                if (m_vlm == null)
                {
                    m_vlm = ObjectFactory.Build<IVideoLanManager>(m_hMediaLib);
                }

                return m_vlm;
            }
        } 

        /// <summary>
        /// Gets list of available audio output modules
        /// </summary>
        public IEnumerable<AudioOutputModuleInfo> AudioOutputModules
        {
            get
            {
                IntPtr pList = LibVlcMethods.libvlc_audio_output_list_get(m_hMediaLib);
                libvlc_audio_output_t pDevice = (libvlc_audio_output_t)Marshal.PtrToStructure(pList, typeof(libvlc_audio_output_t));

                do
                {
                    AudioOutputModuleInfo info = GetDeviceInfo(pDevice);

                    yield return info;
                    if (pDevice.p_next != IntPtr.Zero)
                    {
                        pDevice = (libvlc_audio_output_t)Marshal.PtrToStructure(pDevice.p_next, typeof(libvlc_audio_output_t));
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);

                LibVlcMethods.libvlc_audio_output_list_release(pList);
            }
        }

        /// <summary>
        /// Gets list of available audio output devices
        /// </summary>
        public IEnumerable<AudioOutputDeviceInfo> GetAudioOutputDevices(AudioOutputModuleInfo audioOutputModule)
        {
            int i = LibVlcMethods.libvlc_audio_output_device_count(m_hMediaLib, audioOutputModule.Name.ToUtf8());
            for (int j = 0; j < i; j++)
            {
                AudioOutputDeviceInfo d = new AudioOutputDeviceInfo();
                IntPtr pName = LibVlcMethods.libvlc_audio_output_device_longname(m_hMediaLib, audioOutputModule.Name.ToUtf8(), j);
                d.Longname = Marshal.PtrToStringAnsi(pName);
                IntPtr pId = LibVlcMethods.libvlc_audio_output_device_id(m_hMediaLib, audioOutputModule.Name.ToUtf8(), j);
                d.Id = Marshal.PtrToStringAnsi(pId);

                yield return d;
            }
        }

        private AudioOutputModuleInfo GetDeviceInfo(libvlc_audio_output_t pDevice)
        {
            return new AudioOutputModuleInfo()
            {
                Name = Marshal.PtrToStringAnsi(pDevice.psz_name),
                Description = Marshal.PtrToStringAnsi(pDevice.psz_description)
            };
        }

        private void TrySetVLCPath()
        {
            try
            {
                m_currentDir = Directory.GetCurrentDirectory();
                if (Environment.Is64BitProcess)
                {
                    TrySet64BitPath();
                }
                else
                {
                    TrySetVLCPath("vlc media player");
                }
            }
            catch (Exception ex)
            {
                m_logger.Error("Failed to set VLC path: " + ex.Message);
            }
        }

        private void TrySet64BitPath()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC"))
            {
                object vlcDir = rk.GetValue("InstallDir");
                if (vlcDir != null)
                {
                    Directory.SetCurrentDirectory(vlcDir.ToString());
                }
            }
        }

        private void TrySetVLCPath(string vlcRegistryKey)
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        object DisplayName = sk.GetValue("DisplayName");
                        if (DisplayName != null)
                        {
                            if (DisplayName.ToString().ToLower().IndexOf(vlcRegistryKey.ToLower()) > -1)
                            {
                                object vlcDir = sk.GetValue("InstallLocation");

                                if (vlcDir != null)
                                {
                                    Directory.SetCurrentDirectory(vlcDir.ToString());
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets error message for the last LibVLC error in the calling thread
        /// </summary>
        public string LastErrorMsg
        {
            get 
            {
                IntPtr pError = LibVlcMethods.libvlc_errmsg();
                return Marshal.PtrToStringAnsi(pError);
            }
        }
    }
}
