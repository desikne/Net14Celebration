using System.Collections.Generic;
using System.Data.SqlClient;
using QB.Core.Enums;

namespace QB.Core.Contracts
{
    public class ColumnFilter
    {
        public ColumnFilter()
        {
            this.Params = new List<SqlParameter>();
            this.Operator = StatementOperation.Non;
        }

        public string KeyName { get; set; }

        public string Filter { get; set; }

        public StatementOperation Operator { get; set; }

        public List<SqlParameter> Params { get; protected set; }
    }
}