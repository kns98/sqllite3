//
// Community.CsharpSqlite.SQLiteClient.SqliteDataReader.cs
//
// Provides a means of reading a forward-only stream of rows from a Sqlite 
// database file.
//
// Author(s): Vladimir Vukicevic  <vladimir@pobox.com>
//            Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//	          Joshua Tauberer <tauberer@for.net>
//
// Copyright (C) 2002  Vladimir Vukicevic
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace Community.CsharpSqlite.SQLiteClient
{
    public class SqliteDataReader : DbDataReader, IDataReader, IDisposable, IDataRecord
    {
        #region Constructors and destructors

        internal SqliteDataReader(SqliteCommand cmd, Sqlite3.Vdbe pVm, int version)
        {
            command = cmd;
            rows = new List<object[]>();
            column_names_sens = new Dictionary<string, object>();
            column_names_insens = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            closed = false;
            current_row = -1;
            reading = true;
            ReadpVm(pVm, version, cmd);
            ReadingDone();
        }

        #endregion

        #region Fields

        private readonly SqliteCommand command;
        private readonly List<object[]> rows;
        private string[] columns;
        private readonly Dictionary<string, object> column_names_sens;
        private readonly Dictionary<string, object> column_names_insens;
        private int current_row;
        private bool closed;
        private bool reading;
        private int records_affected;
        private string[] decltypes;

        #endregion

        #region Properties

        public override int Depth => 0;

        public override int FieldCount => columns.Length;

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override object this[int i] => GetValue(i);

        public override bool IsClosed => closed;

        public override int RecordsAffected => records_affected;

        #endregion

        #region Internal Methods

        internal void ReadpVm(Sqlite3.Vdbe pVm, int version, SqliteCommand cmd)
        {
            int pN;
            IntPtr pazValue;
            IntPtr pazColName;
            var first = true;

            int[] declmode = null;

            while (true)
            {
                var hasdata = cmd.ExecuteStatement(pVm, out pN, out pazValue, out pazColName);

                // For the first row, get the column information
                if (first)
                {
                    first = false;

                    if (version == 3)
                    {
                        // A decltype might be null if the type is unknown to sqlite.
                        decltypes = new string[pN];
                        declmode = new int[pN]; // 1 == integer, 2 == datetime
                        for (var i = 0; i < pN; i++)
                        {
                            var decl = Sqlite3.sqlite3_column_decltype(pVm, i);
                            if (decl != null)
                            {
                                decltypes[i] = decl.ToLower(CultureInfo.InvariantCulture);
                                if (decltypes[i] == "int" || decltypes[i] == "integer")
                                    declmode[i] = 1;
                                else if (decltypes[i] == "date" || decltypes[i] == "datetime")
                                    declmode[i] = 2;
                            }
                        }
                    }

                    columns = new string[pN];
                    for (var i = 0; i < pN; i++)
                    {
                        string colName;
                        //if (version == 2) {
                        //	IntPtr fieldPtr = Marshal.ReadIntPtr (pazColName, i*IntPtr.Size);
                        //	colName = Sqlite.HeapToString (fieldPtr, ((SqliteConnection)cmd.Connection).Encoding);
                        //} else {
                        colName = Sqlite3.sqlite3_column_name(pVm, i);
                        //}
                        columns[i] = colName;
                        column_names_sens[colName] = i;
                        column_names_insens[colName] = i;
                    }
                }

                if (!hasdata) break;

                var data_row = new object [pN];
                for (var i = 0; i < pN; i++)
                    /*
                    if (version == 2) {
						IntPtr fieldPtr = Marshal.ReadIntPtr (pazValue, i*IntPtr.Size);
						data_row[i] = Sqlite.HeapToString (fieldPtr, ((SqliteConnection)cmd.Connection).Encoding);
					} else {
                    */
                    switch (Sqlite3.sqlite3_column_type(pVm, i))
                    {
                        case 1:
                            var val = Sqlite3.sqlite3_column_int64(pVm, i);

                            // If the column was declared as an 'int' or 'integer', let's play
                            // nice and return an int (version 3 only).
                            if (declmode[i] == 1 && val >= int.MinValue && val <= int.MaxValue)
                                data_row[i] = (int)val;

                            // Or if it was declared a date or datetime, do the reverse of what we
                            // do for DateTime parameters.
                            else if (declmode[i] == 2)
                                data_row[i] = DateTime.FromFileTime(val);
                            else
                                data_row[i] = val;

                            break;
                        case 2:
                            data_row[i] = Sqlite3.sqlite3_column_double(pVm, i);
                            break;
                        case 3:
                            data_row[i] = Sqlite3.sqlite3_column_text(pVm, i);

                            // If the column was declared as a 'date' or 'datetime', let's play
                            // nice and return a DateTime (version 3 only).
                            if (declmode[i] == 2)
                                if (data_row[i] == null) data_row[i] = null;
                                else data_row[i] = DateTime.Parse((string)data_row[i], CultureInfo.InvariantCulture);
                            break;
                        case 4:
                            var blobbytes = Sqlite3.sqlite3_column_bytes16(pVm, i);
                            var blob = Sqlite3.sqlite3_column_blob(pVm, i);
                            //byte[] blob = new byte[blobbytes];
                            //Marshal.Copy (blobptr, blob, 0, blobbytes);
                            data_row[i] = blob;
                            break;
                        case 5:
                            data_row[i] = null;
                            break;
                        default:
                            throw new Exception("FATAL: Unknown sqlite3_column_type");
                        //}
                    }

                rows.Add(data_row);
            }
        }

        internal void ReadingDone()
        {
            records_affected = command.NumChanges();
            reading = false;
        }

        #endregion

        #region Public Methods

        public override void Close()
        {
            closed = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Close();
        }

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator(this);
        }
#if !SQLITE_SILVERLIGHT
        public override DataTable GetSchemaTable()
        {
            var dataTableSchema = new DataTable();

            dataTableSchema.Columns.Add("ColumnName", typeof(string));
            dataTableSchema.Columns.Add("ColumnOrdinal", typeof(int));
            dataTableSchema.Columns.Add("ColumnSize", typeof(int));
            dataTableSchema.Columns.Add("NumericPrecision", typeof(int));
            dataTableSchema.Columns.Add("NumericScale", typeof(int));
            dataTableSchema.Columns.Add("IsUnique", typeof(bool));
            dataTableSchema.Columns.Add("IsKey", typeof(bool));
            dataTableSchema.Columns.Add("BaseCatalogName", typeof(string));
            dataTableSchema.Columns.Add("BaseColumnName", typeof(string));
            dataTableSchema.Columns.Add("BaseSchemaName", typeof(string));
            dataTableSchema.Columns.Add("BaseTableName", typeof(string));
            dataTableSchema.Columns.Add("DataType", typeof(Type));
            dataTableSchema.Columns.Add("AllowDBNull", typeof(bool));
            dataTableSchema.Columns.Add("ProviderType", typeof(int));
            dataTableSchema.Columns.Add("IsAliased", typeof(bool));
            dataTableSchema.Columns.Add("IsExpression", typeof(bool));
            dataTableSchema.Columns.Add("IsIdentity", typeof(bool));
            dataTableSchema.Columns.Add("IsAutoIncrement", typeof(bool));
            dataTableSchema.Columns.Add("IsRowVersion", typeof(bool));
            dataTableSchema.Columns.Add("IsHidden", typeof(bool));
            dataTableSchema.Columns.Add("IsLong", typeof(bool));
            dataTableSchema.Columns.Add("IsReadOnly", typeof(bool));

            dataTableSchema.BeginLoadData();
            for (var i = 0; i < FieldCount; i += 1)
            {
                var schemaRow = dataTableSchema.NewRow();

                schemaRow["ColumnName"] = columns[i];
                schemaRow["ColumnOrdinal"] = i;
                schemaRow["ColumnSize"] = 0;
                schemaRow["NumericPrecision"] = 0;
                schemaRow["NumericScale"] = 0;
                schemaRow["IsUnique"] = false;
                schemaRow["IsKey"] = false;
                schemaRow["BaseCatalogName"] = "";
                schemaRow["BaseColumnName"] = columns[i];
                schemaRow["BaseSchemaName"] = "";
                schemaRow["BaseTableName"] = "";
                schemaRow["DataType"] = typeof(string);
                schemaRow["AllowDBNull"] = true;
                schemaRow["ProviderType"] = 0;
                schemaRow["IsAliased"] = false;
                schemaRow["IsExpression"] = false;
                schemaRow["IsIdentity"] = false;
                schemaRow["IsAutoIncrement"] = false;
                schemaRow["IsRowVersion"] = false;
                schemaRow["IsHidden"] = false;
                schemaRow["IsLong"] = false;
                schemaRow["IsReadOnly"] = false;

                dataTableSchema.Rows.Add(schemaRow);
                schemaRow.AcceptChanges();
            }

            dataTableSchema.EndLoadData();

            return dataTableSchema;
        }
#endif
        public override bool NextResult()
        {
            current_row++;

            return current_row < rows.Count;
        }

        public override bool Read()
        {
            return NextResult();
        }

        #endregion

        #region IDataRecord getters

        public override bool GetBoolean(int i)
        {
            return Convert.ToBoolean(rows[current_row][i]);
        }

        public override byte GetByte(int i)
        {
            return Convert.ToByte(rows[current_row][i]);
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            var data = (byte[])rows[current_row][i];
            if (buffer != null)
                Array.Copy(data, (int)fieldOffset, buffer, bufferOffset, length);
#if (SQLITE_SILVERLIGHT||WINDOWS_MOBILE)
            return data.Length - fieldOffset;
#else
            return data.LongLength - fieldOffset;
#endif
        }

        public override char GetChar(int i)
        {
            return Convert.ToChar(rows[current_row][i]);
        }

        public override long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            var data = (char[])rows[current_row][i];
            if (buffer != null)
                Array.Copy(data, (int)fieldOffset, buffer, bufferOffset, length);
#if (SQLITE_SILVERLIGHT||WINDOWS_MOBILE)
            return data.Length - fieldOffset;
#else
            return data.LongLength - fieldOffset;
#endif
        }

        public override string GetDataTypeName(int i)
        {
            if (decltypes != null && decltypes[i] != null)
                return decltypes[i];
            return "text"; // SQL Lite data type
        }

        public override DateTime GetDateTime(int i)
        {
            return Convert.ToDateTime(rows[current_row][i]);
        }

        public override decimal GetDecimal(int i)
        {
            return Convert.ToDecimal(rows[current_row][i]);
        }

        public override double GetDouble(int i)
        {
            return Convert.ToDouble(rows[current_row][i]);
        }

        public override Type GetFieldType(int i)
        {
            var row = current_row;
            if (row == -1 && rows.Count == 0) return typeof(string);
            if (row == -1) row = 0;
            var element = rows[row][i];
            if (element != null)
                return element.GetType();
            return typeof(string);

            // Note that the return value isn't guaranteed to
            // be the same as the rows are read if different
            // types of information are stored in the column.
        }

        public override float GetFloat(int i)
        {
            return Convert.ToSingle(rows[current_row][i]);
        }

        public override Guid GetGuid(int i)
        {
            var value = GetValue(i);
            if (!(value is Guid))
            {
                if (value is DBNull)
                    throw new SqliteExecutionException("Column value must not be null");
                throw new InvalidCastException("Type is " + value.GetType());
            }

            return (Guid)value;
        }

        public override short GetInt16(int i)
        {
            return Convert.ToInt16(rows[current_row][i]);
        }

        public override int GetInt32(int i)
        {
            return Convert.ToInt32(rows[current_row][i]);
        }

        public override long GetInt64(int i)
        {
            return Convert.ToInt64(rows[current_row][i]);
        }

        public override string GetName(int i)
        {
            return columns[i];
        }

        public override int GetOrdinal(string name)
        {
            var v = column_names_sens.ContainsKey(name) ? column_names_sens[name] : null;
            if (v == null)
                v = column_names_insens.ContainsKey(name) ? column_names_insens[name] : null;
            if (v == null)
                throw new ArgumentException("Column does not exist.");
            return (int)v;
        }

        public override string GetString(int i)
        {
            if (rows[current_row][i] != null)
                return rows[current_row][i].ToString();
            return null;
        }

        public override object GetValue(int i)
        {
            return rows[current_row][i];
        }

        public override int GetValues(object[] values)
        {
            var num_to_fill = Math.Min(values.Length, columns.Length);
            for (var i = 0; i < num_to_fill; i++)
                if (rows[current_row][i] != null)
                    values[i] = rows[current_row][i];
                else
                    values[i] = DBNull.Value;
            return num_to_fill;
        }

        public override bool IsDBNull(int i)
        {
            return rows[current_row][i] == null;
        }

        public override bool HasRows => rows.Count > 0;

        public override int VisibleFieldCount => FieldCount;

        #endregion
    }
}