using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace ER_Model
{
    public class Database
    {
        OracleConnection Connection { get; set; }
        public Database()
        {
            Connection = new OracleConnection();
            Connection.ConnectionString = "User Id=system;Password=1234;Data Source=127.0.0.1:1521/ORA12C";
            Connection.Open();
        }

        public ERModel GetERModel(
            string owner,
            string tableName,
            ERModel erModel,
            int depth)
        {
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = $@"SELECT
  columnMetaData.owner columnMetaData_owner,
  columnMetaData.table_name columnMetaData_table_name,
  columnMetaData.column_name columnMetaData_column_name,
  columnMetaData.nullable,
  refData.referencedOwner,
  refData.referencedTableName,
  refData.referencedColumnName
from
    all_tab_columns columnMetaData
left join
    (select
        referencingTable.owner as referencingOwner,
        referencingTable.table_name as referencingTableName,
        referencingTable.column_name as referencingColumnName,
        referencedTable.owner as referencedOwner,
        referencedTable.table_name as referencedTableName,
        referencedTable.column_name as referencedColumnName,
	referencingTable.column_name
     from all_cons_columns referencingTable
     join all_cons_columns referencedTable on referencingTable.position = referencedTable.position
     join all_constraints
         on referencingTable.owner = all_constraints.owner
        and referencedTable.constraint_name = all_constraints.r_constraint_name
        and referencedTable.owner = all_constraints.r_owner
        and referencingTable.constraint_name = all_constraints.constraint_name
        and all_constraints.constraint_type = 'R') refData
    on columnMetaData.owner = referencingOwner
       and columnMetaData.table_name = referencingTableName
       and columnMetaData.column_name = referencingColumnName

where

      columnMetaData.owner = upper('{owner}')
  and columnMetaData.table_name = upper('{tableName}')";
                List<ReferenceColumn> toReferences = new List<ReferenceColumn>();
                List<Column> normalColumns = new List<Column>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(4))
                        {
                            normalColumns.Add(
                                new Column
                                {
                                    ColumnName = reader.GetString(2),
                                    ColumnNullable = reader.GetString(3) == "Y",
                                    ColumnType = ""
                                });
                        }
                        else
                        {
                            toReferences.Add(
                                new ReferenceColumn
                                {
                                    ColumnName = reader.GetString(2),
                                    ColumnNullable = reader.GetString(3) == "Y",
                                    ReferenceOwnerName = reader.GetString(4),
                                    ReferenceTableName = reader.GetString(5),
                                    ReferenceColumnName =  reader.GetString(6)
                                });
                        }
                    }
                }
                Table table =
                    new Table
                    {
                        Owner = owner.ToUpper(),
                        Name = tableName.ToUpper(),
                    };

                if(!erModel.Tables.ContainsKey(table))
                {
                    erModel.Tables.Add(
                        table,
                        new TableColumns
                        {
                            To = toReferences,
                            NormalColumns = normalColumns
                        });
                }
                if (depth > 0)
                {
                    foreach (var t in toReferences)
                    {
                        GetERModel(t.ReferenceOwnerName, t.ReferenceTableName, erModel, depth - 1);
                    }
                }
                return erModel;
            }
        }
    }
}