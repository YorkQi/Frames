using System.Threading;
using System.Threading.Tasks;

namespace Frame.EventBus
{
    public partial interface IEventBus
    {
        /// <summary>
        /// 执行事件
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        internal Task Run(CancellationToken cancel);
    }
}
