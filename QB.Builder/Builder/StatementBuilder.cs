using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using QB.Core.Contracts;
using QB.Core.Entities.Base;
using QB.Core.Enums;

namespace QB.Builder.Builder
{
    public class StatementBuilder
    {
        private readonly Dictionary<StatementOperation, Func<string, string, string>> statementBuilder = new Dictionary<StatementOperation, Func<string, string, string>>
        {
            [StatementOperation.Non] = (key, value) => $"{value}",
            [StatementOperation.In] = (key, value) => $"{key} IN ({value})",
            [StatementOperation.NotIn] = (key, value) => $"{key} NOT IN ({value})",
            [StatementOperation.Equals] = (key, value) => $"{key} = {value}",
            [StatementOperation.NotEquals] = (key, value) => $"{key} != {value}",
            [StatementOperation.Less] = (key, value) => $"{key} < {value}",
            [StatementOperation.More] = (key, value) => $"{key} > {value}",
            [StatementOperation.LessAndEquals] = (key, value) => $"{key} <= {value}",
            [StatementOperation.MoreAndEquals] = (key, value) => $"{key} >= {value}",
            [StatementOperation.Like] = (key, value) => $"{key} LIKE '%' + {value} + '%'"
        };

        private readonly QueryBuilder queryBuilder = new QueryBuilder();

        public string Build(string key, string value, StatementOperation operation)
        {
            return this.statementBuilder[operation].Invoke(key, value);
        }

        public ColumnFilter BuildStatement<TEntity>(string key, string[] value) where TEntity : BaseEntity
        {
            var baseTableName = this.queryBuilder.GetTableName<TEntity>();
            var result = new ColumnFilter();
            result.KeyName = $"{baseTableName}.{key}";
            var filterValue = $"@{result.Params.Count}";
            result.Params.Add(new SqlParameter($"@{result.Params.Count}", value.First()));
            foreach (var s in value.Skip(1))
            {
                filterValue = $"{filterValue}, @{result.Params.Count}";
                result.Params.Add(new SqlParameter($"@{result.Params.Count}", s));
            }

            result.Filter = this.statementBuilder[StatementOperation.In].Invoke(key, filterValue);

            return result;
        }

        public ColumnFilter BuildStatement<TEntity>(string key, int value, StatementOperation operation = StatementOperation.Equals) where TEntity : BaseEntity
        {
            return this.BuildStatement<TEntity>(key, value.ToString(), operation);
        }

        public ColumnFilter BuildStatement<TEntity>(string key, string value, StatementOperation operation = StatementOperation.Equals) where TEntity : BaseEntity
        {
            var baseTableName = this.queryBuilder.GetTableName<TEntity>();
            var result = new ColumnFilter();
            result.KeyName = $"{baseTableName}.{key}";
            result.Filter = this.statementBuilder[operation].Invoke(result.KeyName, $"@{result.Params.Count}");
            result.Params.Add(new SqlParameter($"@{result.Params.Count}", value));

            return result;
        }

        public ColumnFilter BuildAndStatement(List<ColumnFilter> filters)
        {
            var result = new ColumnFilter();
            var firstFilter = filters.First();
            result.Filter = $"({this.statementBuilder[firstFilter.Operator].Invoke(firstFilter.KeyName, firstFilter.Filter)}";
            result.Params.AddRange(firstFilter.Params);
            var filterIndex = result.Params.Count;
            foreach (var columnFilter in filters.Skip(1))
            {
                columnFilter.Filter = this.NormalizeParameters(columnFilter.Filter, filterIndex);
                result.Filter = $"{result.Filter} AND {this.statementBuilder[columnFilter.Operator].Invoke(columnFilter.KeyName, columnFilter.Filter)}";
                result.Params.AddRange(columnFilter.Params);
                filterIndex += columnFilter.Params.Count;
            }

            result.Filter = $"{result.Filter})";
            return result;
        }

        public ColumnFilter BuildOrFilters(List<ColumnFilter> filters)
        {
            var result = new ColumnFilter();
            var firstFilter = filters.First();
            result.Filter = $"({this.statementBuilder[firstFilter.Operator].Invoke(firstFilter.KeyName, firstFilter.Filter)}";
            result.Params.AddRange(firstFilter.Params);
            var filterIndex = result.Params.Count;
            foreach (var columnFilter in filters.Skip(1))
            {
                columnFilter.Filter = this.NormalizeParameters(columnFilter.Filter, filterIndex);
                result.Filter = $"{result.Filter} OR {this.statementBuilder[columnFilter.Operator].Invoke(columnFilter.KeyName, columnFilter.Filter)}";
                result.Params.AddRange(columnFilter.Params);
                filterIndex += columnFilter.Params.Count;
            }

            result.Filter = $"{result.Filter})";
            return result;
        }

        public string NormalizeParameters(string query, int index)
        {
            var regex = new Regex(@"@\d+");
            query = regex.Replace(query, x =>
            {
                var number = int.Parse(x.Value.Substring(1)) + index;
                return $"@{number}";
            });

            return query;
        }
    }
}