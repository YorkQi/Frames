namespace Frame.Core.Entitys
{
    public interface IEntity
    {

    }

    public interface IEntity<TPrimaryKey> : IEntity
    {
        [Key]
        public TPrimaryKey Id { get; set; }
    }
}
