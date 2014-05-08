using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Declarations.Media
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICompositeMemoryInputMedia : IMedia
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamInfo"></param>
        /// <param name="maxItemsInQueue"></param>
        void AddStream(StreamInfo streamInfo, int maxItemsInQueue = 30);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="frame"></param>
        void AddFrame(int streamId, FrameData frame);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="data"></param>
        /// <param name="pts"></param>
        /// <param name="dts"></param>
        void AddFrame(int streamId, byte[] data, long pts, long dts = -1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="bitmap"></param>
        /// <param name="pts"></param>
        /// <param name="dts"></param>
        void AddFrame(int streamId, Bitmap bitmap, long pts, long dts = -1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="sound"></param>
        /// <param name="dts"></param>
        void AddFrame(int streamId, Sound sound, long dts = -1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        void SetExceptionHandler(Action<Exception> handler);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <returns></returns>
        int GetPendingFramesCount(int streamId);

        /// <summary>
        /// 
        /// </summary>
        void StreamAddingComplete();
    }
}
