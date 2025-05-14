using System;

namespace Frame.Databases.Entitys
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
        public KeyAttribute()
        {

        }
    }
}
