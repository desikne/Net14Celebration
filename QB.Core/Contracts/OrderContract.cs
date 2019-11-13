using QB.Core.Enums;

namespace QB.Core.Contracts
{
    public class OrderContract
    {
        public string ColumnName { get; set; }

        public OrderDirection Direction { get; set; }
    }
}