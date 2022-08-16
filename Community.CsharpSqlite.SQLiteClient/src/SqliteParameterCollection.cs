//
// Community.CsharpSqlite.SQLiteClient.SqliteParameterCollection.cs
//
// Represents a collection of parameters relevant to a SqliteCommand as well as 
// their respective mappings to columns in a DataSet.
//
//Author(s):		Vladimir Vukicevic  <vladimir@pobox.com>
//			Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//			Chris Turchin <chris@turchin.net>
//			Jeroen Zwartepoorte <jeroen@xs4all.nl>
//			Thomas Zoechling <thomas.zoechling@gmx.at>
//          Alex West <alxwest@gmail.com>       
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

namespace Community.CsharpSqlite.SQLiteClient
{
    public class SqliteParameterCollection : DbParameterCollection
    {
        #region Fields

        private readonly List<SqliteParameter> numeric_param_list = new List<SqliteParameter>();
        private readonly Dictionary<string, int> named_param_hash = new Dictionary<string, int>();

        #endregion

        #region Private Methods

        private void CheckSqliteParam(object value)
        {
            if (!(value is SqliteParameter))
                throw new InvalidCastException("Can only use SqliteParameter objects");
            var sqlp = value as SqliteParameter;
            if (sqlp.ParameterName == null || sqlp.ParameterName.Length == 0)
                sqlp.ParameterName = GenerateParameterName();
        }

        private void RecreateNamedHash()
        {
            for (var i = 0; i < numeric_param_list.Count; i++)
                named_param_hash[numeric_param_list[i].ParameterName] = i;
        }

        //FIXME: if the user is calling Insert at various locations with unnamed parameters, this is not going to work....
        private string GenerateParameterName()
        {
            var index = Count + 1;
            var name = string.Empty;

            while (index > 0)
            {
                name = ":" + index;
                if (IndexOf(name) == -1)
                    index = -1;
                else
                    index++;
            }

            return name;
        }

        #endregion

        #region Properties

        private bool isPrefixed(string parameterName)
        {
            return parameterName.Length > 1 && (parameterName[0] == ':' || parameterName[0] == '$');
        }

        protected override DbParameter GetParameter(int parameterIndex)
        {
            if (Count >= parameterIndex + 1)
                return numeric_param_list[parameterIndex];
            throw new IndexOutOfRangeException("The specified parameter index does not exist: " + parameterIndex);
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            if (Contains(parameterName))
                return this[named_param_hash[parameterName]];
            if (isPrefixed(parameterName) && Contains(parameterName.Substring(1)))
                return this[named_param_hash[parameterName.Substring(1)]];
            throw new IndexOutOfRangeException("The specified name does not exist: " + parameterName);
        }

        protected override void SetParameter(int parameterIndex, DbParameter parameter)
        {
            if (Count >= parameterIndex + 1)
                numeric_param_list[parameterIndex] = (SqliteParameter)parameter;
            else
                throw new IndexOutOfRangeException("The specified parameter index does not exist: " + parameterIndex);
        }

        protected override void SetParameter(string parameterName, DbParameter parameter)
        {
            if (Contains(parameterName))
                numeric_param_list[named_param_hash[parameterName]] = (SqliteParameter)parameter;
            else if (parameterName.Length > 1 && Contains(parameterName.Substring(1)))
                numeric_param_list[named_param_hash[parameterName.Substring(1)]] = (SqliteParameter)parameter;
            else
                throw new IndexOutOfRangeException("The specified name does not exist: " + parameterName);
        }

        public override int Count => numeric_param_list.Count;

        public override bool IsSynchronized => ((IList)numeric_param_list).IsSynchronized;

        public override bool IsFixedSize => ((IList)numeric_param_list).IsFixedSize;

        public override bool IsReadOnly => ((IList)numeric_param_list).IsReadOnly;

        public override object SyncRoot => ((IList)numeric_param_list).SyncRoot;

        #endregion

        #region Public Methods

        public override void AddRange(Array values)
        {
            if (values == null || values.Length == 0)
                return;

            foreach (var value in values)
                Add(value);
        }

        public override int Add(object value)
        {
            CheckSqliteParam(value);
            var sqlp = value as SqliteParameter;
            if (named_param_hash.ContainsKey(sqlp.ParameterName))
                throw new DuplicateNameException(
                    "Parameter collection already contains the a SqliteParameter with the given ParameterName.");
            numeric_param_list.Add(sqlp);
            named_param_hash.Add(sqlp.ParameterName, numeric_param_list.IndexOf(sqlp));
            return named_param_hash[sqlp.ParameterName];
        }

        public SqliteParameter Add(SqliteParameter param)
        {
            Add((object)param);
            return param;
        }

        public SqliteParameter Add(string name, object value)
        {
            return Add(new SqliteParameter(name, value));
        }

        public SqliteParameter Add(string name, DbType type)
        {
            return Add(new SqliteParameter(name, type));
        }

        public override void Clear()
        {
            numeric_param_list.Clear();
            named_param_hash.Clear();
        }

        public override void CopyTo(Array array, int index)
        {
            numeric_param_list.CopyTo((SqliteParameter[])array, index);
        }

        public override bool Contains(object value)
        {
            return Contains((SqliteParameter)value);
        }

        public override bool Contains(string parameterName)
        {
            return named_param_hash.ContainsKey(parameterName);
        }

        public bool Contains(SqliteParameter param)
        {
            return Contains(param.ParameterName);
        }

        public override IEnumerator GetEnumerator()
        {
            return numeric_param_list.GetEnumerator();
        }

        public override int IndexOf(object param)
        {
            return IndexOf((SqliteParameter)param);
        }

        public override int IndexOf(string parameterName)
        {
            if (isPrefixed(parameterName))
            {
                var sub = parameterName.Substring(1);
                if (named_param_hash.ContainsKey(sub))
                    return named_param_hash[sub];
            }

            if (named_param_hash.ContainsKey(parameterName))
                return named_param_hash[parameterName];
            return -1;
        }

        public int IndexOf(SqliteParameter param)
        {
            return IndexOf(param.ParameterName);
        }

        public override void Insert(int index, object value)
        {
            CheckSqliteParam(value);
            if (numeric_param_list.Count == index)
            {
                Add(value);
                return;
            }

            numeric_param_list.Insert(index, (SqliteParameter)value);
            RecreateNamedHash();
        }

        public override void Remove(object value)
        {
            CheckSqliteParam(value);
            RemoveAt((SqliteParameter)value);
        }

        public override void RemoveAt(int index)
        {
            RemoveAt(numeric_param_list[index].ParameterName);
        }

        public override void RemoveAt(string parameterName)
        {
            if (!named_param_hash.ContainsKey(parameterName))
                throw new ApplicationException("Parameter " + parameterName + " not found");

            numeric_param_list.RemoveAt(named_param_hash[parameterName]);
            named_param_hash.Remove(parameterName);

            RecreateNamedHash();
        }

        public void RemoveAt(SqliteParameter param)
        {
            RemoveAt(param.ParameterName);
        }

        #endregion
    }
}