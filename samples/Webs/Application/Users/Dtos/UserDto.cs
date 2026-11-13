using System;
using Domain.Users.Enums;

namespace Application.Users.Dtos
{
    /// <summary>用户对外模型。</summary>
    public sealed record UserDto(
        int Id,
        string Name,
        UserSex Sex,
        string ProfilePicture,
        DateTime CreationTime,
        DateTime? LastModificationTime,
        bool IsDeleted);
}
