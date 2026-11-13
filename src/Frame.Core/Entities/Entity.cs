using System;
using System.Collections.Generic;

namespace Frame.Core.Entities
{
    /// <summary>
    /// 实体基类，提供主键 ID 和基于 ID 的相等性比较。
    /// 主键类型支持 int/long/Guid/string 等实现了 <see cref="IEquatable{T}"/> 的类型。
    /// 实体类可通过实现可选审计接口获得自动审计能力：
    /// <list type="bullet">
    ///   <item><see cref="IHasCreationTime"/> — 自动记录创建时间</item>
    ///   <item><see cref="IModificationAuditedObject"/> — 自动记录修改时间</item>
    ///   <item><see cref="ISoftDelete"/> — 软删除标记</item>
    ///   <item><see cref="IDeletionAuditedObject"/> — 删除时间审计</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TPrimaryKey">主键类型，需实现 <see cref="IEquatable{T}"/></typeparam>
    public abstract class Entity<TPrimaryKey> : IEntity<TPrimaryKey>, IHasDomainEvents
        where TPrimaryKey : IEquatable<TPrimaryKey>
    {
        /// <summary>实体唯一标识。新实体在不赋值时使用该类型的默认值（如 0、Guid.Empty、null）。</summary>
        public TPrimaryKey Id { get; set; } = default!;

        /// <summary>
        /// 领域事件集合。由 <c>AddDomainEvent</c> 添加，在 UOW 提交前自动分发。
        /// 用于解耦同一聚合内的副作用逻辑。
        /// </summary>
        private List<object>? _domainEvents;

        /// <summary>获取当前已注册的领域事件列表（只读）</summary>
        public IReadOnlyList<object>? DomainEvents => _domainEvents?.AsReadOnly();

        /// <summary>
        /// 注册一个领域事件。事件将在工作单元（UOW）提交前被分发。
        /// </summary>
        /// <param name="eventData">事件数据对象</param>
        protected void AddDomainEvent(object eventData)
        {
            _domainEvents ??= [];
            _domainEvents.Add(eventData);
        }

        /// <summary>
        /// 移除指定领域事件。
        /// </summary>
        protected void RemoveDomainEvent(object eventData)
        {
            _domainEvents?.Remove(eventData);
        }

        /// <summary>
        /// 清空所有已注册的领域事件。
        /// </summary>
        protected void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        // ===========================================================================
        // IHasDomainEvents 显式实现：供数据库层在保存后收集/清空事件
        // ===========================================================================

        IReadOnlyList<object>? IHasDomainEvents.DomainEvents => DomainEvents;

        void IHasDomainEvents.ClearDomainEvents() => ClearDomainEvents();

        /// <summary>判断当前实体是否为新建（尚未持久化）。主键为默认值时视为新实体。</summary>
        public virtual bool IsTransient() => EqualityComparer<TPrimaryKey>.Default.Equals(Id, default!);

        // ===========================================================================
        // 实体相等性比较：基于主键 ID 和类型兼容性
        // ===========================================================================

        public override bool Equals(object? obj)
        {
            if (obj is null || obj is not Entity<TPrimaryKey>)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var other = (Entity<TPrimaryKey>)obj;
            var typeOfThis = GetType();
            var typeOfOther = other.GetType();
            if (!typeOfThis.IsAssignableFrom(typeOfOther) && !typeOfOther.IsAssignableFrom(typeOfThis))
                return false;

            // 双方均为新实体（Id 为默认值）时视为不相等
            if (IsTransient() && other.IsTransient())
                return false;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            // 新实体的哈希基于类型，避免默认 Id 导致冲突
            if (IsTransient())
                return GetType().GetHashCode();

            return Id.GetHashCode();
        }

        public override string ToString() => $"[{GetType().Name} {Id}]";

        public static bool operator ==(Entity<TPrimaryKey> left, Entity<TPrimaryKey> right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TPrimaryKey> left, Entity<TPrimaryKey> right) => !(left == right);
    }
}