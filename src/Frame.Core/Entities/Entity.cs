using System;

namespace Frame.Core.Entities
{
    public abstract class Entity<TPrimaryKey> : IEntity<TPrimaryKey>
        where TPrimaryKey : struct, IEquatable<TPrimaryKey>
    {
        public TPrimaryKey Id { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null || obj is not Entity<TPrimaryKey>)
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
            if (!typeOfThis.IsAssignableFrom(typeOfOther) && !typeOfOther.IsAssignableFrom(typeOfThis))
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
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TPrimaryKey> left, Entity<TPrimaryKey> right)
        {
            return !(left == right);
        }
    }
}
