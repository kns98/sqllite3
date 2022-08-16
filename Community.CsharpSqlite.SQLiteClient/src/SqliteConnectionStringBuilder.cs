//
// Community.CsharpSqlite.SQLiteClient.SqliteConnectionStringBuilder.cs
//
// Author(s):
//   Sureshkumar T (tsureshkumar@novell.com)
//   Marek Habersack (grendello@gmail.com)
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2007 Marek Habersack
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
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Community.CsharpSqlite.SQLiteClient
{
    public sealed class SqliteConnectionStringBuilder : DbConnectionStringBuilder
    {
        private const string DEF_URI = null;
        private const int DEF_MODE = 0644;
        private const int DEF_VERSION = 2;
        private const Encoding DEF_ENCODING = null;
        private const int DEF_BUSYTIMEOUT = 0;

        #region // Fields

        private string _uri;
        private int _mode;
        private int _version;
        private Encoding _encoding;
        private int _busy_timeout;

        private static readonly Dictionary<string, string> _keywords; // for mapping duplicate keywords

        #endregion // Fields

        #region Constructors

        public SqliteConnectionStringBuilder() : this(string.Empty)
        {
        }

        public SqliteConnectionStringBuilder(string connectionString)
        {
            Init();
            ConnectionString = connectionString;
        }

        static SqliteConnectionStringBuilder()
        {
            _keywords = new Dictionary<string, string>();
            _keywords["URI"] = "Uri";
            _keywords["DATA SOURCE"] = "Data Source";
            _keywords["DATASOURCE"] = "Data Source";
            _keywords["URI"] = "Data Source";
            _keywords["MODE"] = "Mode";
            _keywords["VERSION"] = "Version";
            _keywords["BUSY TIMEOUT"] = "Busy Timeout";
            _keywords["BUSYTIMEOUT"] = "Busy Timeout";
            _keywords["ENCODING"] = "Encoding";
        }

        #endregion // Constructors

        #region Properties

        public string DataSource
        {
            get => _uri;
            set
            {
                base["Data Source"] = value;
                _uri = value;
            }
        }

        public string Uri
        {
            get => _uri;
            set
            {
                base["Data Source"] = value;
                _uri = value;
            }
        }

        public int Mode
        {
            get => _mode;
            set
            {
                base["Mode"] = value;
                _mode = value;
            }
        }

        public int Version
        {
            get => _version;
            set
            {
                base["Version"] = value;
                _version = value;
            }
        }

        public int BusyTimeout
        {
            get => _busy_timeout;
            set
            {
                base["Busy Timeout"] = value;
                _busy_timeout = value;
            }
        }

        public Encoding Encoding
        {
            get => _encoding;
            set
            {
                base["Encoding"] = value;
                _encoding = value;
            }
        }

        public override bool IsFixedSize => true;

        public override object this[string keyword]
        {
            get
            {
                var mapped = MapKeyword(keyword);
                return base[mapped];
            }
            set => SetValue(keyword, value);
        }

        public override ICollection Keys => base.Keys;

        public override ICollection Values => base.Values;

        #endregion // Properties

        #region Methods

        private void Init()
        {
            _uri = DEF_URI;
            _mode = DEF_MODE;
            _version = DEF_VERSION;
            _encoding = DEF_ENCODING;
            _busy_timeout = DEF_BUSYTIMEOUT;
        }

        public override void Clear()
        {
            base.Clear();
            Init();
        }

        public override bool ContainsKey(string keyword)
        {
            keyword = keyword.ToUpper().Trim();
            if (_keywords.ContainsKey(keyword))
                return base.ContainsKey(_keywords[keyword]);
            return false;
        }

        public override bool Remove(string keyword)
        {
            if (!ContainsKey(keyword))
                return false;
            this[keyword] = null;
            return true;
        }

        public override bool TryGetValue(string keyword, out object value)
        {
            if (!ContainsKey(keyword))
            {
                value = string.Empty;
                return false;
            }

            return base.TryGetValue(_keywords[keyword.ToUpper().Trim()], out value);
        }

        #endregion // Methods

        #region Private Methods

        private string MapKeyword(string keyword)
        {
            keyword = keyword.ToUpper().Trim();
            if (!_keywords.ContainsKey(keyword))
                throw new ArgumentException("Keyword not supported :" + keyword);
            return _keywords[keyword];
        }

        private void SetValue(string key, object value)
        {
            if (key == null)
                throw new ArgumentNullException("key cannot be null!");

            var mappedKey = MapKeyword(key);

            switch (mappedKey.ToUpper(CultureInfo.InvariantCulture).Trim())
            {
                case "DATA SOURCE":
                    if (value == null)
                    {
                        _uri = DEF_URI;
                        base.Remove(mappedKey);
                    }
                    else
                    {
                        Uri = value.ToString();
                    }

                    break;

                case "MODE":
                    if (value == null)
                    {
                        _mode = DEF_MODE;
                        base.Remove(mappedKey);
                    }
                    else
                    {
                        Mode = ConvertToInt32(value);
                    }

                    break;

                case "VERSION":
                    if (value == null)
                    {
                        _version = DEF_MODE;
                        base.Remove(mappedKey);
                    }
                    else
                    {
                        Version = ConvertToInt32(value);
                    }

                    break;

                case "BUSY TIMEOUT":
                    if (value == null)
                    {
                        _busy_timeout = DEF_BUSYTIMEOUT;
                        base.Remove(mappedKey);
                    }
                    else
                    {
                        BusyTimeout = ConvertToInt32(value);
                    }

                    break;

                case "ENCODING":
                    if (value == null)
                    {
                        _encoding = DEF_ENCODING;
                        base.Remove(mappedKey);
                    }
                    else if (value is string)
                    {
                        Encoding = Encoding.GetEncoding((string)value);
                    }
                    else
                    {
                        throw new ArgumentException("Cannot set encoding from a non-string argument");
                    }

                    break;

                default:
                    throw new ArgumentException("Keyword not supported :" + key);
            }
        }

        private static int ConvertToInt32(object value)
        {
            return int.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        private static bool ConvertToBoolean(object value)
        {
            if (value == null)
                throw new ArgumentNullException("null value cannot be converted to boolean");
            var upper = value.ToString().ToUpper().Trim();
            if (upper == "YES" || upper == "TRUE")
                return true;
            if (upper == "NO" || upper == "FALSE")
                return false;
            throw new ArgumentException(string.Format("Invalid boolean value: {0}", value));
        }

        #endregion // Private Methods
    }
}