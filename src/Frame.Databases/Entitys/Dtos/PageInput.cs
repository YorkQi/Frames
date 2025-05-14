namespace Frame.Databases.Entitys.Dtos
{
    public class PageInput
    {
        /// <summary>
        /// 页
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 行数
        /// </summary>
        public int Limit { get; set; } = 20;
    }
}
