namespace Frame.Core.Entitys
{
    public interface IEntity
    {

    }

    public interface IEntity<TPrimaryKey> : IEntity
    {
        public TPrimaryKey Id { get; set; }
    }
}
