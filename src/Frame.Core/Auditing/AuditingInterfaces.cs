using System;

namespace Frame.Core.Auditing
{
    /// <summary>
    /// 记录创建时间的实体契约。
    /// </summary>
    public interface IHasCreationTime
    {
        /// <summary>创建时间（UTC）。</summary>
        DateTime CreationTime { get; set; }
    }

    /// <summary>
    /// 记录创建人审计信息的实体契约。
    /// </summary>
    public interface ICreationAuditedObject : IHasCreationTime
    {
        /// <summary>创建人标识。</summary>
        string? CreatorId { get; set; }
    }

    /// <summary>
    /// 记录最后修改时间的实体契约。
    /// </summary>
    public interface IHasModificationTime
    {
        /// <summary>最后修改时间（UTC）。未修改过时为 <c>null</c>。</summary>
        DateTime? LastModificationTime { get; set; }
    }

    /// <summary>
    /// 记录最后修改人审计信息的实体契约。
    /// </summary>
    public interface IModificationAuditedObject : IHasModificationTime
    {
        /// <summary>最后修改人标识。</summary>
        string? LastModifierId { get; set; }
    }

    /// <summary>
    /// 软删除契约。实现该接口的实体在删除时只置位 <see cref="IsDeleted"/>，不物理删除。
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>是否已被软删除。</summary>
        bool IsDeleted { get; set; }
    }

    /// <summary>
    /// 记录删除时间的实体契约。
    /// </summary>
    public interface IHasDeletionTime
    {
        /// <summary>删除时间（UTC）。未删除时为 <c>null</c>。</summary>
        DateTime? DeletionTime { get; set; }
    }

    /// <summary>
    /// 记录删除人审计信息的实体契约。
    /// </summary>
    public interface IDeletionAuditedObject : IHasDeletionTime
    {
        /// <summary>删除人标识。</summary>
        string? DeleterId { get; set; }
    }

    /// <summary>
    /// 完整的创建/修改审计契约。
    /// </summary>
    public interface IAuditedObject : ICreationAuditedObject, IModificationAuditedObject
    {
    }

    /// <summary>
    /// 完整的创建/修改/删除审计契约（含软删除）。
    /// </summary>
    public interface IFullAuditedObject : IAuditedObject, IDeletionAuditedObject, ISoftDelete
    {
    }
}
