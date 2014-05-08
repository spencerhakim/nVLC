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

using System;
using System.Runtime.InteropServices;
using Declarations.Events;
using LibVlcWrapper;

namespace Implementation.Events
{
    internal abstract class EventManager : DisposableBase
    {
        protected IEventProvider m_eventProvider;
        VlcEventHandlerDelegate m_callback;
        IntPtr m_hCallback;

        protected EventManager(IEventProvider eventProvider)
        {
            m_eventProvider = eventProvider;

            m_callback = MediaPlayerEventOccured;
            m_hCallback = Marshal.GetFunctionPointerForDelegate(m_callback);
        }

        protected void Attach(libvlc_event_e eType)
        {
            if (NativeMethods.libvlc_event_attach(m_eventProvider.EventManagerHandle, eType, m_hCallback, IntPtr.Zero) != 0)
            {
                throw new OutOfMemoryException("Failed to subscribe to event notification");
            }
        }

        protected void Dettach(libvlc_event_e eType)
        {
            NativeMethods.libvlc_event_detach(m_eventProvider.EventManagerHandle, eType, m_hCallback, IntPtr.Zero);
        }

        protected abstract void MediaPlayerEventOccured(ref libvlc_event_t libvlc_event, IntPtr userData);

        protected override void Dispose(bool disposing)
        {
            //
        }
    }
}
