using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Users.Enums;
using Domain.Users.Events;
using Frame.Core.Auditing;
using Frame.Core.Entities;

namespace Domain.Users
{
    /// <summary>
    /// 用户聚合根。
    /// 审计字段（创建/修改/软删除）由框架在保存时自动填充，业务代码不要手工赋值。
    /// </summary>
    [Table("Users")]
    public class User : Entity<int>, IFullAuditedObject
    {
        /// <summary>用户名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public UserSex Sex { get; set; }

        /// <summary>头像地址</summary>
        public string ProfilePicture { get; set; } = string.Empty;

        /// <summary>创建时间（UTC，框架填充）</summary>
        public DateTime CreationTime { get; set; }

        /// <summary>创建人（框架填充）</summary>
        public string? CreatorId { get; set; }

        /// <summary>最后修改时间（UTC，框架填充）</summary>
        public DateTime? LastModificationTime { get; set; }

        /// <summary>最后修改人（框架填充）</summary>
        public string? LastModifierId { get; set; }

        /// <summary>是否已软删除（框架在删除时置为 true）</summary>
        public bool IsDeleted { get; set; }

        /// <summary>删除时间（UTC，框架填充）</summary>
        public DateTime? DeletionTime { get; set; }

        /// <summary>删除人（框架填充）</summary>
        public string? DeleterId { get; set; }

        /// <summary>
        /// 创建用户。注册领域事件，事件会在工作单元提交后分发。
        /// </summary>
        public static User Create(string name, UserSex sex, string? profilePicture = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("用户名称不能为空。", nameof(name));

            var user = new User
            {
                Name = name.Trim(),
                Sex = sex,
                ProfilePicture = profilePicture?.Trim() ?? string.Empty
            };

            user.AddDomainEvent(new UserRegisteredDomainEvent(user.Name));
            return user;
        }

        /// <summary>修改用户名称，名称未变化时不产生事件。</summary>
        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("用户名称不能为空。", nameof(name));

            var trimmed = name.Trim();
            if (string.Equals(Name, trimmed, StringComparison.Ordinal))
                return;

            Name = trimmed;
            AddDomainEvent(new UserRenamedDomainEvent(Id, trimmed));
        }
    }
}
