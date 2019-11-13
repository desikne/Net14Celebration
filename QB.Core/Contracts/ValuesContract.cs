using System.Collections.Generic;
using System.Linq;

namespace QB.Core.Contracts
{
    public class ValuesContract<TFilter>
    {
        public IEnumerable<TFilter> Contains { get; set; }

        public IEnumerable<ColumnFilter> Select { get; internal set; }

        public ValuesContract()
        {
            this.Contains = new List<TFilter>();
        }

        public bool IsEmpty()
        {
            return this.Contains == null || !this.Contains.Any();
        }

        public ValuesContract<TFilter> Clone()
        {
            return new ValuesContract<TFilter>
            {
                Contains = this.Contains.ToList()
            };
        }
    }
}