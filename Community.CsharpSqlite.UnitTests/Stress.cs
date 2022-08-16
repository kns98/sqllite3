using System;
using System.Diagnostics;
using System.IO;
using Community.CsharpSqlite;
using Xunit;

namespace Community.CsharpSQLite.UnitTests
{
    public class Stress
    {
        public static void Main(string[] args)
        {
            OpenDB(databaseName);
            Insert_1000();
            Console.ReadLine();
        }


        private const string databaseName = "test.db";

        private const string INSERT_Command =
            "INSERT INTO Root VALUES (?,?)";

        private static readonly string[] CREATE_Commands =
        {
            "CREATE TABLE Root (intIndex INTEGER PRIMARY KEY, strIndex TEXT)",
            "CREATE INDEX RootStrIndex ON Root (strIndex)"
        };

        private static readonly string[] PRAGMA_Commands =
        {
            "PRAGMA synchronous =  OFF",
            "PRAGMA temp_store =  MEMORY",
            "PRAGMA journal_mode = OFF",
            "PRAGMA locking_mode=EXCLUSIVE"
        };

        public Stress()
        {
            var db = OpenDB(databaseName);
            InitializeTables(db);
            db.CloseDatabase();
        }

        private static SQLiteDatabase OpenDB(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            var db = new SQLiteDatabase(fileName);

            for (var i = 0; i < PRAGMA_Commands.Length; i++) db.ExecuteNonQuery(PRAGMA_Commands[i]);

            return db;
        }

        private void InitializeTables(SQLiteDatabase db)
        {
            db.ExecuteNonQuery("BEGIN EXCLUSIVE");
            for (var i = 0; i < CREATE_Commands.Length; i++) db.ExecuteNonQuery(CREATE_Commands[i]);
        }

        [Fact]
        public static void InsertRecords()
        {
            var db = OpenDB(databaseName);

            db.ExecuteNonQuery("BEGIN");
            var stmt = new SQLiteVdbe(db, INSERT_Command);
            long key = 1999;
            for (var i = 0; i < 1000; i++)
            {
                key = (3141592621L * key + 2718281829L) % 1000000007L;
                stmt.Reset();
                stmt.BindLong(1, key);
                stmt.BindText(2, key.ToString());
                stmt.ExecuteStep();
            }

            stmt.Close();
            db.ExecuteNonQuery("END");

            db.CloseDatabase();
        }

        [Fact]
        public static void Insert_1000()
        {
            for (var i = 0; i < 1000; i++)
            {
                Debug.WriteLine("Round " + i);
                InsertRecords();
            }
        }
    }
}