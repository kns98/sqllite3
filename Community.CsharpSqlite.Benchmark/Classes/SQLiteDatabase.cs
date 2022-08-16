//  $Header$

using System;
using System.Collections;
using System.Data;

namespace Community.CsharpSqlite
{
    using sqlite = Sqlite3.sqlite3;

    /// <summary>
    ///     C#-SQLite wrapper with functions for opening, closing and executing queries.
    /// </summary>
    public class SQLiteDatabase
    {
        // pointer to database
        private sqlite db;

        /// <summary>
        ///     Creates new instance of SQLiteBase class with no database attached.
        /// </summary>
        public SQLiteDatabase()
        {
            db = null;
        }

        /// <summary>
        ///     Creates new instance of SQLiteDatabase class and opens database with given name.
        /// </summary>
        /// <param name="DatabaseName">Name (and path) to SQLite database file</param>
        public SQLiteDatabase(string DatabaseName)
        {
            OpenDatabase(DatabaseName);
        }

        /// <summary>
        ///     Opens database.
        /// </summary>
        /// <param name="DatabaseName">Name of database file</param>
        public void OpenDatabase(string DatabaseName)
        {
            // opens database 
            if (
#if NET_35
                Sqlite3.sqlite3_open
#else
Sqlite3.sqlite3_open
#endif
                    (DatabaseName, out db) != Sqlite3.SQLITE_OK)
            {
                // if there is some error, database pointer is set to 0 and exception is throws
                db = null;
                throw new Exception("Error with opening database " + DatabaseName + "!");
            }
        }

        /// <summary>
        ///     Closes opened database.
        /// </summary>
        public void CloseDatabase()
        {
            // closes the database if there is one opened
            if (db != null)
            {
#if NET_35
                Sqlite3.sqlite3_close
#else
Sqlite3.sqlite3_close
#endif
                    (db);
            }
        }

        /// <summary>
        ///     Returns connection
        /// </summary>
        public sqlite Connection()
        {
            return db;
        }

        /// <summary>
        ///     Returns the list of tables in opened database.
        /// </summary>
        /// <returns></returns>
        public ArrayList GetTables()
        {
            // executes query that select names of all tables in master table of the database
            var query = "SELECT name FROM sqlite_master " +
                        "WHERE type = 'table'" +
                        "ORDER BY 1";
            var table = ExecuteQuery(query);

            // Return all table names in the ArrayList
            var list = new ArrayList();
            foreach (DataRow row in table.Rows) list.Add(row.ItemArray[0].ToString());
            return list;
        }

        /// <summary>
        ///     Executes query that does not return anything (e.g. UPDATE, INSERT, DELETE).
        /// </summary>
        /// <param name="query"></param>
        public void ExecuteNonQuery(string query)
        {
            // calles SQLite function that executes non-query
            Sqlite3.exec(db, query, 0, 0, 0);
            // if there is error, excetion is thrown
            if (db.errCode != Sqlite3.SQLITE_OK)
                throw new Exception("Error with executing non-query: \"" + query + "\"!\n" +
#if NET_35
                                    Sqlite3.sqlite3_errmsg
#else
Sqlite3.sqlite3_errmsg
#endif
                                        (db));
        }

        /// <summary>
        ///     Executes query that does return something (e.g. SELECT).
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public DataTable ExecuteQuery(string query)
        {
            // compiled query
            var statement = new SQLiteVdbe(this, query);

            // table for result of query
            var table = new DataTable();

            // create new instance of DataTable with name "resultTable"
            table = new DataTable("resultTable");

            // reads rows
            do
            {
            } while (ReadNextRow(statement.VirtualMachine(), table) == Sqlite3.SQLITE_ROW);

            // finalize executing this query
            statement.Close();

            // returns table
            return table;
        }

        // private function for reading rows and creating table and columns
        private int ReadNextRow(Sqlite3.Vdbe vm, DataTable table)
        {
            var columnCount = table.Columns.Count;
            if (columnCount == 0)
                if ((columnCount = ReadColumnNames(vm, table)) == 0)
                    return Sqlite3.SQLITE_ERROR;

            int resultType;
            if ((resultType =
#if NET_35
                    Sqlite3.sqlite3_step
#else
Sqlite3.sqlite3_step
#endif
                        (vm)) == Sqlite3.SQLITE_ROW)
            {
                var columnValues = new object[columnCount];

                for (var i = 0; i < columnCount; i++)
                {
                    var columnType =
#if NET_35
                        Sqlite3.sqlite3_column_type
#else
Sqlite3.sqlite3_column_type
#endif
                            (vm, i);
                    switch (columnType)
                    {
                        case Sqlite3.SQLITE_INTEGER:
                        {
                            table.Columns[i].DataType = typeof(long);
                            columnValues[i] =
#if NET_35
                                Sqlite3.sqlite3_column_int
#else
Sqlite3.sqlite3_column_int
#endif
                                    (vm, i);
                            break;
                        }
                        case Sqlite3.SQLITE_FLOAT:
                        {
                            table.Columns[i].DataType = typeof(double);
                            columnValues[i] =
#if NET_35
                                Sqlite3.sqlite3_column_double
#else
Sqlite3.sqlite3_column_double
#endif
                                    (vm, i);
                            break;
                        }
                        case Sqlite3.SQLITE_TEXT:
                        {
                            table.Columns[i].DataType = typeof(string);
                            columnValues[i] =
#if NET_35
                                Sqlite3.sqlite3_column_text
#else
Sqlite3.sqlite3_column_text
#endif
                                    (vm, i);
                            break;
                        }
                        case Sqlite3.SQLITE_BLOB:
                        {
                            table.Columns[i].DataType = typeof(byte[]);
                            columnValues[i] =
#if NET_35
                                Sqlite3.sqlite3_column_blob
#else
Sqlite3.sqlite3_column_blob
#endif
                                    (vm, i);
                            break;
                        }
                        default:
                        {
                            table.Columns[i].DataType = null;
                            columnValues[i] = "";
                            break;
                        }
                    }
                }

                table.Rows.Add(columnValues);
            }

            return resultType;
        }

        // private function for creating Column Names
        // Return number of colums read
        private int ReadColumnNames(Sqlite3.Vdbe vm, DataTable table)
        {
            var columnName = "";
            var columnType = 0;
            // returns number of columns returned by statement
            var columnCount =
#if NET_35
                Sqlite3.sqlite3_column_count
#else
Sqlite3.sqlite3_column_count
#endif
                    (vm);
            var columnValues = new object[columnCount];

            try
            {
                // reads columns one by one
                for (var i = 0; i < columnCount; i++)
                {
                    columnName =
#if NET_35
                        Sqlite3.sqlite3_column_name
#else
Sqlite3.sqlite3_column_name
#endif
                            (vm, i);
                    columnType =
#if NET_35
                        Sqlite3.sqlite3_column_type
#else
Sqlite3.sqlite3_column_type
#endif
                            (vm, i);

                    switch (columnType)
                    {
                        case Sqlite3.SQLITE_INTEGER:
                        {
                            // adds new integer column to table
                            table.Columns.Add(columnName, Type.GetType("System.Int64"));
                            break;
                        }
                        case Sqlite3.SQLITE_FLOAT:
                        {
                            table.Columns.Add(columnName, Type.GetType("System.Double"));
                            break;
                        }
                        case Sqlite3.SQLITE_TEXT:
                        {
                            table.Columns.Add(columnName, Type.GetType("System.String"));
                            break;
                        }
                        case Sqlite3.SQLITE_BLOB:
                        {
                            table.Columns.Add(columnName, Type.GetType("System.byte[]"));
                            break;
                        }
                        default:
                        {
                            table.Columns.Add(columnName, Type.GetType("System.String"));
                            break;
                        }
                    }
                }
            }
            catch
            {
                return 0;
            }

            return table.Columns.Count;
        }
    }
}