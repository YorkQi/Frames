using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Frame.Core.Entitys
{
    public abstract class Entity : IEntity
    {

    }

    public abstract class Entity<TPrimaryKey> : Entity, IEntity<TPrimaryKey>
    {
        [NotNull]
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
        [Key]
        public TPrimaryKey Id { get; set; } = default;
#pragma warning restore CS8601 // 引用类型赋值可能为 null。

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Entity<TPrimaryKey>))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = (Entity<TPrimaryKey>)obj;
            var typeOfThis = GetType();
            var typeOfOther = other.GetType();
            if (!typeOfThis.GetTypeInfo().IsAssignableFrom(typeOfOther) && !typeOfOther.GetTypeInfo().IsAssignableFrom(typeOfThis))
            {
                return false;
            }

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<TPrimaryKey> left, Entity<TPrimaryKey> right)
        {
            if (Equals(left, null))
            {
                return Equals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TPrimaryKey> left, Entity<TPrimaryKey> right)
        {
            return !(left == right);
        }
    }
}
