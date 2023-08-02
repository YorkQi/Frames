using System;

namespace Frame.Core.Exceptions
{
    [Flags]
    public enum FrameExceptionCode
    {
        /// <summary>
        /// 传入参数不合法
        /// </summary>
        Invalidate = 00_00_00_00
    }
}
