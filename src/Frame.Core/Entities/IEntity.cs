namespace Frame.Core.Entities
{
    /// <summary>
    /// 实体标记接口。所有实体（含聚合根）必须直接或间接实现此接口。
    /// </summary>
    public interface IEntity
    {
    }

    /// <summary>
    /// 泛型实体接口，定义主键属性。
    /// </summary>
    /// <typeparam name="TPrimaryKey">
    /// 主键类型。框架支持 int、long、Guid、string 等实现了 IEquatable&lt;T&gt; 的类型。
    /// </typeparam>
    public interface IEntity<TPrimaryKey> : IEntity
    {
        /// <summary>实体唯一标识。主键值由数据库生成或手动赋值。</summary>
        TPrimaryKey Id { get; set; }
    }
}
