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
using Declarations.Media;
using Implementation.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Implementation.Media
{
    internal sealed unsafe class CompositeMemoryInputMedia : BasicMedia, ICompositeMemoryInputMedia
    {
        Dictionary<int, StreamData> m_streamData = new Dictionary<int, StreamData>();
        Action<Exception> m_excHandler;
        bool m_isComplete = false;

        public CompositeMemoryInputMedia(IntPtr hMediaLib)
            : base(hMediaLib)
        {

        }

        public void StreamAddingComplete()
        {
            m_isComplete = true;
        }

        public void AddStream(StreamInfo streamInfo, int maxItemsInQueue = 30)
        {
            if (m_isComplete)
            {
                throw new InvalidOperationException("Stream adding is complete. No more streams allowed");
            }

            m_streamData[streamInfo.ID] = new StreamData(streamInfo, maxItemsInQueue);
        }

        public void AddFrame(int streamId, FrameData frame)
        {
            var clone = DeepClone(frame);
            m_streamData[streamId].Queue.Add(clone);
        }

        public void AddFrame(int streamId, byte[] data, long pts, long dts = -1)
        {
            var clone = DeepClone(data);
            clone.PTS = pts;
            clone.DTS = dts;
            m_streamData[streamId].Queue.Add(clone);
        }

        public void AddFrame(int streamId, Bitmap bitmap, long pts, long dts = -1)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            FrameData frame = DeepClone(bmpData.Scan0, bmpData.Stride * bmpData.Height);
            bitmap.UnlockBits(bmpData);
            frame.PTS = pts;
            frame.DTS = dts;
            m_streamData[streamId].Queue.Add(frame);
        }

        public void AddFrame(int streamId, Sound sound, long dts = -1)
        {
            var clone = DeepClone(sound);
            clone.DTS = dts;
            m_streamData[streamId].Queue.Add(clone);
        }

        public void SetExceptionHandler(Action<Exception> handler)
        {
            m_excHandler = handler;
        }

        public int GetPendingFramesCount(int streamId)
        {
            return m_streamData[streamId].Queue.Count;
        }

        private FrameData DeepClone(byte[] buffer)
        {
            FrameData clone = new FrameData();
            clone.Data = new IntPtr(MemoryHeap.Alloc(buffer.Length));
            Marshal.Copy(buffer, 0, clone.Data, buffer.Length);
            clone.DataSize = buffer.Length;
            return clone;
        }

        private FrameData DeepClone(FrameData frameData)
        {
            FrameData clone = DeepClone(frameData.Data, frameData.DataSize);
            clone.DTS = frameData.DTS;
            clone.PTS = frameData.PTS;
            return clone;
        }

        private FrameData DeepClone(Sound sound)
        {
            FrameData clone = DeepClone(sound.SamplesData, (int)sound.SamplesSize);
            clone.PTS = sound.Pts;
            return clone;
        }

        private FrameData DeepClone(IntPtr data, int size)
        {
            FrameData clone = new FrameData();
            clone.Data = new IntPtr(MemoryHeap.Alloc(size));
            MemoryHeap.CopyMemory(clone.Data.ToPointer(), data.ToPointer(), size);
            clone.DataSize = size;
            return clone;
        }

        private class StreamData
        {
            public BlockingCollection<FrameData> Queue;
            public StreamInfo StreamInfo;

            public StreamData(StreamInfo streamInfo, int maxQueueSize)
            {
                StreamInfo = streamInfo;
                Queue = new BlockingCollection<FrameData>(maxQueueSize);
            }
        }

        private int OnImemGet(void* data, char* cookie, long* dts, long* pts, int* flags, uint* dataSize, void** ppData)
        {
            try
            {
                FrameData fdata = GetNextFrameData(cookie);
                *ppData = fdata.Data.ToPointer();
                *dataSize = (uint)fdata.DataSize;
                *pts = fdata.PTS;
                *dts = fdata.DTS;
                *flags = 0;
                return 0;
            }
            catch (Exception ex)
            {
                if (m_excHandler != null)
                {
                    m_excHandler(ex);
                }
                else
                {
                    throw new Exception("imem-get callback failed", ex);
                }
                return 1;
            }
        }

        private FrameData GetNextFrameData(char* cookie)
        {
            int index = (int)*cookie;
            return m_streamData[index].Queue.Take();
        }

        private void OnImemRelease(void* data, char* cookie, uint dataSize, void* pData)
        {
            try
            {
                MemoryHeap.Free(pData);
            }
            catch (Exception ex)
            {
                if (m_excHandler != null)
                {
                    m_excHandler(ex);
                }
                else
                {
                    throw new Exception("imem-release callback failed", ex);
                }
            }
        }
    }
}
