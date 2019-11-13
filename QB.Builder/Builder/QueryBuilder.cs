using System;
using System.Collections.Generic;
using System.Linq;
using QB.Core.Attributes;
using QB.Core.Contracts;
using QB.Core.Entities.Base;
using QB.Core.Enums;

namespace QB.Builder.Builder
{
    internal class QueryBuilder
    {
        private const string SelectKey = "SELECT";
        private const string FromKey = "FROM ";
        private const string WhereKey = "WHERE";
        private const string GroupByKey = "GROUP BY ";
        private const string OrderByKey = "ORDER BY ";
        private const string AscKey = "ASC";
        private const string DescKey = "DESC";

        public string DeleteMultiKeyQuery<T>(T entity) where T : BaseEntity
        {
            string sql = string.Empty;
            string whereStatement = this.GetWhereStatement(entity);
            string tableName = this.GetTableName<T>();

            sql = $"DELETE FROM {tableName} WHERE {whereStatement} ";

            return sql;
        }

        public string GetTableName<T>() where T : BaseEntity
        {
            return this.GetTableName(typeof(T));
        }

        internal string GetTableName(Type type)
        {
            string tableName = type.Name;
            TableNameAttribute tableAttribute = (TableNameAttribute)Attribute.GetCustomAttribute(type, typeof(TableNameAttribute));
            if (tableAttribute != null)
            {
                tableName = tableAttribute.Value;
            }

            return tableName;
        }

        private string GetWhereStatement<T>(T entity) where T : BaseEntity
        {
            var props = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(PrimaryKeyColumnAttribute)));
            if (props.Any())
            {
                var filters = new List<string>(props.Count());

                foreach (var propertyInfo in props)
                {
                    var value = entity.GetType().GetProperty(propertyInfo.Name).GetValue(entity, null);
                    filters.Add($"{propertyInfo.Name} = {(value is string ? ("'" + value + "'") : value)}");
                }

                return filters.Aggregate((i, j) => $"{i} AND {j}");
            }
            else
            {
                throw new Exception($"No PrimaryKeyColumnAttribute on any column in {typeof(T).Name}");
            }
        }

        public string UpdateMultiKeyQuery<T>(T entity) where T : BaseEntity
        {
            string sql = string.Empty;
            string whereStatement = this.GetWhereStatement(entity);
            string setStatement = this.GetSetStatement(entity);
            string tableName = this.GetTableName<T>();

            sql = $"UPDATE {tableName} SET {setStatement} WHERE {whereStatement} ";

            return sql;
        }

        private string GetSetStatement<T>(T entity) where T : BaseEntity
        {
            var updateProps = typeof(T).GetProperties().Where(prop => !Attribute.IsDefined(prop, typeof(PrimaryKeyColumnAttribute))
                                                                      && !Attribute.IsDefined(prop, typeof(ResultColumnAttribute))).ToList();

            if (updateProps.Any())
            {
                var valueSets = new List<string>(updateProps.Count());

                foreach (var propertyInfo in updateProps)
                {
                    var value = entity.GetType().GetProperty(propertyInfo.Name).GetValue(entity, null);
                    var setVal = value is string ? $"'{value}'" : value;
                    valueSets.Add($"{propertyInfo.Name} = {setVal}");
                }

                return valueSets.Aggregate((i, j) => $"{i} , {j}");
            }
            else
            {
                throw new Exception($"No PrimaryKeyColumnAttribute on any column in {typeof(T).Name}");
            }
        }

        internal string InsertFilterInQuery(string query, string filterCommand)
        {
            int groupByIndex = query.IndexOf(GroupByKey);
            int orderByIndex = query.IndexOf(OrderByKey);
            if (query.Equals(WhereKey))
            {
                query = $"{query} {filterCommand}";
            }
            else if (!query.Contains(WhereKey))
            {
                query = this.InsertWhereKeyWord(query, filterCommand);
            }
            else if (groupByIndex > 0)
            {
                query = query.Insert(groupByIndex, $" AND {filterCommand} ");
            }
            else if (orderByIndex > 0)
            {
                query = query.Insert(orderByIndex, $" AND {filterCommand} ");
            }
            else
            {
                query = $"{query} AND {filterCommand}";
            }

            return query;
        }

        private string InsertWhereKeyWord(string query, string filterCommand)
        {
            int groupByIndex = query.IndexOf(GroupByKey);
            if (groupByIndex > 0)
            {
                query = query.Insert(groupByIndex, $"{Environment.NewLine}{WhereKey} {filterCommand} ");
            }
            else
            {
                int orderByIndex = query.IndexOf(OrderByKey);
                if (orderByIndex > 0)
                {
                    query = query.Insert(orderByIndex, $"{Environment.NewLine}{WhereKey} {filterCommand} ");
                }
                else
                {
                    query = $"{query}{Environment.NewLine}{WhereKey} {filterCommand}";
                }
            }

            return query;
        }

        internal string SetSelector<TEntity>(string query, params string[] columns) where TEntity : BaseEntity
        {
            var selectTableName = this.GetTableName<TEntity>();
            columns = columns.Select(q => $"{selectTableName}.{q}").ToArray();
            var resultColumns = string.Join($",{Environment.NewLine}", columns);

            int fromByIndex = query.IndexOf(FromKey);
            if (fromByIndex < 0)
            {
                var tableName = this.GetTableName<TEntity>();
                query = $"{SelectKey} {resultColumns}{Environment.NewLine} {FromKey} {tableName} AS {tableName}{Environment.NewLine} {query}";
            }
            else
            {
                query = $"{SelectKey} {resultColumns}{Environment.NewLine} {query.Substring(fromByIndex)}{Environment.NewLine}";
            }

            return query;
        }

        internal string SetDistinctSelector<TEntity>(string query, params string[] columns) where TEntity : BaseEntity
        {
            var selectTableName = this.GetTableName<TEntity>();
            columns = columns.Select(q => $"{selectTableName}.{q}").ToArray();
            var resultColumns = string.Join($",{Environment.NewLine}", columns);

            int fromByIndex = query.IndexOf(FromKey);
            if (fromByIndex < 0)
            {
                var tableName = this.GetTableName<TEntity>();
                query = $"{SelectKey} DISTINCT {resultColumns}{Environment.NewLine} {FromKey} {tableName} AS {tableName}{Environment.NewLine} {query}";
            }
            else
            {
                query = $"{SelectKey} DISTINCT {resultColumns}{Environment.NewLine} {query.Substring(fromByIndex)}{Environment.NewLine}";
            }

            return query;
        }

        internal string InsertSelector<TEntity>(string query, params string[] columns) where TEntity : BaseEntity
        {
            var tableName = this.GetTableName<TEntity>();
            columns = columns.Select(q => $"{tableName}.{q}").ToArray();
            var resultColumns = string.Join($",{Environment.NewLine}", columns);
            int fromByIndex = query.IndexOf(FromKey);
            return query.Insert(fromByIndex, $",{resultColumns}{Environment.NewLine}");
        }

        internal string InsertSelector(string query, params string[] columns)
        {
            columns = columns.Select(q => $"{q}").ToArray();
            var resultColumns = string.Join($",{Environment.NewLine}", columns);
            int fromByIndex = query.IndexOf(FromKey);
            return query.Insert(fromByIndex, $",{resultColumns}{Environment.NewLine}");
        }

        public string InsertCoalesceSelector<TEntity>(string query, string alias, string[] columns) where TEntity : BaseEntity
        {
            var resultColumns = string.Join($",{Environment.NewLine}", columns);
            int fromByIndex = query.IndexOf(FromKey);
            return query.Insert(fromByIndex, $",COALESCE({resultColumns}) AS {alias}{Environment.NewLine}");
        }

        internal string InsertJoin<TEntity, TTarget>(string query, string key, string targetKey) where TEntity : BaseEntity
            where TTarget : BaseEntity
        {
            return this.InsertJoin<TEntity, TTarget>(query, key, targetKey, "JOIN");
        }

        public string InsertLeftJoin<TEntity, TTarget>(string query, string key, string targetKey) where TEntity : BaseEntity where TTarget : BaseEntity
        {
            return this.InsertJoin<TEntity, TTarget>(query, key, targetKey, "LEFT JOIN");
        }

        public string InsertLeftJoin<TEntity, TTarget>(string query, string key, string targetKey, string filterCommand) where TEntity : BaseEntity where TTarget : BaseEntity
        {
            return this.InsertJoin<TEntity, TTarget>(query, key, targetKey, "LEFT JOIN", filterCommand);
        }

        private string InsertJoin<TEntity, TTarget>(string query, string key, string targetKey, string joinType, string filterCommand = null) where TEntity : BaseEntity
            where TTarget : BaseEntity
        {
            int fromByIndex = query.IndexOf(FromKey);
            if (fromByIndex < 0)
            {
                query = this.SetSelector<TEntity>(query, "*");
            }

            var targetTable = this.GetTableName<TTarget>();
            var baseTableName = this.GetTableName<TEntity>();
            int whereIndex = query.IndexOf(WhereKey);
            var filter = string.Empty;
            if (!string.IsNullOrEmpty(filterCommand))
            {
                filter = $" AND {filterCommand}";
            }

            if (whereIndex > 0)
            {
                query = query.Insert(whereIndex, $" {joinType} {targetTable} AS {targetTable} ON {baseTableName}.{key} = {targetTable}.{targetKey} {filter} {Environment.NewLine}");
            }
            else
            {
                query = $"{query} {joinType} {targetTable} AS {targetTable} ON {baseTableName}.{key} = {targetTable}.{targetKey} {filter} {Environment.NewLine} ";
            }

            return query;
        }

        internal string InsertOrder(string query, OrderContract order)
        {
            var ordering = order.Direction == OrderDirection.Ascending ? AscKey : DescKey;
            return $"{query} {Environment.NewLine}{OrderByKey} {order.ColumnName} {ordering}";
        }

        internal string InsertOrder<TEntity>(string query, IEnumerable<OrderContract> sorters) where TEntity : BaseEntity
        {
            var tableName = this.GetTableName<TEntity>();
            Func<OrderDirection, string> mapper = od => (od == OrderDirection.Ascending) ? AscKey : DescKey;
            var lsColumn = sorters.Select(o => $"{tableName}.{o.ColumnName} {mapper(o.Direction)}");

            return $"{query} {Environment.NewLine}{OrderByKey} {string.Join(", ", lsColumn)}";
        }

        internal string InsertPaging(string query, int skip, int take)
        {
            int orderByIndex = query.IndexOf(OrderByKey);
            if (orderByIndex < 0)
            {
                query = $"{query} {Environment.NewLine}{OrderByKey} 1";
            }

            return $"{query} {Environment.NewLine}  OFFSET {skip} ROWS {Environment.NewLine} FETCH NEXT {take} ROWS ONLY";
        }
    }
}