using System;

namespace Frame.Repository.Entitys
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
        public KeyAttribute()
        {

        }
    }
}
