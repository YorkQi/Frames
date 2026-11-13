using System.Collections.Generic;

namespace Frame.Core.Entities
{
    /// <summary>
    /// 携带领域事件的实体契约。
    /// 数据库层在保存前通过该接口收集事件，保存成功后分发并清空。
    /// <see cref="Entity{TPrimaryKey}"/> 已实现该接口，实体内部用 <c>AddDomainEvent</c> 添加事件。
    /// </summary>
    public interface IHasDomainEvents
    {
        /// <summary>当前挂起的领域事件（只读）。</summary>
        IReadOnlyList<object>? DomainEvents { get; }

        /// <summary>清空所有已挂起的领域事件。</summary>
        void ClearDomainEvents();
    }
}
