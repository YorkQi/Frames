using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Projects.Events;
using Frame.Core.Auditing;
using Frame.Core.Entities;

namespace Domain.Projects
{
    /// <summary>
    /// 项目聚合根：项目编码在未删除范围内唯一，归档后不可再改名。
    /// 审计字段由框架在保存时填充。
    /// </summary>
    [Table("Projects")]
    public class Project : Entity<int>, IFullAuditedObject
    {
        /// <summary>项目编码（大写，未删除范围内唯一）</summary>
        public string Code { get; private set; } = string.Empty;

        /// <summary>项目名称</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>项目描述</summary>
        public string? Description { get; private set; }

        /// <summary>负责人标识</summary>
        public string OwnerId { get; private set; } = string.Empty;

        /// <summary>项目状态</summary>
        public ProjectStatus Status { get; private set; }

        /// <inheritdoc />
        public DateTime CreationTime { get; set; }

        /// <inheritdoc />
        public string? CreatorId { get; set; }

        /// <inheritdoc />
        public DateTime? LastModificationTime { get; set; }

        /// <inheritdoc />
        public string? LastModifierId { get; set; }

        /// <inheritdoc />
        public bool IsDeleted { get; set; }

        /// <inheritdoc />
        public DateTime? DeletionTime { get; set; }

        /// <inheritdoc />
        public string? DeleterId { get; set; }

        /// <summary>供 EF Core 物化使用。</summary>
        private Project()
        {
        }

        /// <summary>创建项目并登记“项目已创建”事件。</summary>
        public static Project Create(string code, string name, string? description, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new DomainException("项目编码不能为空。");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("项目名称不能为空。");

            if (string.IsNullOrWhiteSpace(ownerId))
                throw new DomainException("项目负责人不能为空。");

            var project = new Project
            {
                Code = code.Trim().ToUpperInvariant(),
                Name = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                OwnerId = ownerId.Trim(),
                Status = ProjectStatus.Active
            };

            project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Code, project.Name));
            return project;
        }

        /// <summary>改名；名称未变化时不产生领域事件。</summary>
        public void Rename(string name)
        {
            if (Status == ProjectStatus.Archived)
                throw new DomainException("项目已归档，不能改名。");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("项目名称不能为空。");

            var trimmed = name.Trim();
            if (string.Equals(Name, trimmed, StringComparison.Ordinal))
                return;

            Name = trimmed;
            AddDomainEvent(new ProjectRenamedDomainEvent(Code, Name));
        }

        /// <summary>归档；重复归档视为领域错误。</summary>
        public void Archive()
        {
            if (Status == ProjectStatus.Archived)
                throw new DomainException("项目已归档，不能重复归档。");

            Status = ProjectStatus.Archived;
            AddDomainEvent(new ProjectArchivedDomainEvent(Code));
        }
    }
}
