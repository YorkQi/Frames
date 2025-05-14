using System;
using System.Reflection;

namespace Frame.Databases.Entitys
{
    public abstract class Entity<TPrimaryKey> : IEntity, IEntity<TPrimaryKey>
        where TPrimaryKey : struct, IComparable, IEquatable<TPrimaryKey>
    {
        [Key]
        public TPrimaryKey Id { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not Entity<TPrimaryKey>)
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
