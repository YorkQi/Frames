using System;

namespace Frame.Core.Auditing
{
    /// <summary>
    /// 审计字段填充器。数据库层在持久化前调用，把时间与当前用户写入实体。
    /// </summary>
    public interface IAuditPropertySetter
    {
        /// <summary>填充创建时间/创建人（实体新增时调用）。</summary>
        void SetCreationProperties(object entity);

        /// <summary>填充最后修改时间/最后修改人（实体更新时调用）。</summary>
        void SetModificationProperties(object entity);

        /// <summary>填充软删除标记、删除时间/删除人（实体删除时调用）。</summary>
        void SetDeletionProperties(object entity);
    }

    /// <summary>
    /// <see cref="IAuditPropertySetter"/> 的默认实现。
    /// 实体的审计能力由接口决定：只实现 <see cref="IHasCreationTime"/> 的实体只写入创建时间。
    /// </summary>
    public sealed class AuditPropertySetter : IAuditPropertySetter
    {
        private readonly IClock _clock;
        private readonly ICurrentUser _currentUser;

        /// <summary>构造函数。</summary>
        /// <param name="clock">时间提供者</param>
        /// <param name="currentUser">当前用户访问器</param>
        public AuditPropertySetter(IClock clock, ICurrentUser currentUser)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc />
        public void SetCreationProperties(object entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            if (entity is IHasCreationTime hasCreationTime)
            {
                hasCreationTime.CreationTime = _clock.Now;
            }

            if (entity is ICreationAuditedObject auditedObject)
            {
                auditedObject.CreatorId = _currentUser.Id;
            }
        }

        /// <inheritdoc />
        public void SetModificationProperties(object entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            if (entity is IHasModificationTime hasModificationTime)
            {
                hasModificationTime.LastModificationTime = _clock.Now;
            }

            if (entity is IModificationAuditedObject auditedObject)
            {
                auditedObject.LastModifierId = _currentUser.Id;
            }
        }

        /// <inheritdoc />
        public void SetDeletionProperties(object entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            if (entity is ISoftDelete softDelete)
            {
                softDelete.IsDeleted = true;
            }

            if (entity is IHasDeletionTime hasDeletionTime)
            {
                hasDeletionTime.DeletionTime = _clock.Now;
            }

            if (entity is IDeletionAuditedObject auditedObject)
            {
                auditedObject.DeleterId = _currentUser.Id;
            }
        }
    }
}
