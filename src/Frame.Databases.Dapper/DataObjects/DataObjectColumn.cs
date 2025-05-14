namespace Frame.Databases.Dapper.DataObjects
{
    public class DataObjectColumn
    {
        public string Name { get; set; } = string.Empty;

        public object? Value { get; set; }
    }
}
