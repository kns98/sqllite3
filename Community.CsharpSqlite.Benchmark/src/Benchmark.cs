//  $Header$

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using Community.CsharpSqlite;
using Microsoft.Data.Sqlite;

/*
* Benchmark Test for both SQLite and C#-SQLite
*/

public class Benchmark
{
    private static int nRecords;

    private static readonly string[] PRAGMA_Commands =
    {
        "PRAGMA synchronous =  OFF",
        "PRAGMA temp_store =  MEMORY",
        "PRAGMA journal_mode = OFF",
        "PRAGMA locking_mode=EXCLUSIVE"
    };

    private static readonly string[] CREATE_Commands =
    {
        "CREATE TABLE Root (intIndex INTEGER PRIMARY KEY, strIndex TEXT)",
        "CREATE INDEX RootStrIndex ON Root (strIndex)"
    };

    private static readonly string INSERT_Command = "INSERT INTO Root VALUES (?,?)";
    private static readonly string SELECT_Bind_i = "SELECT * FROM Root WHERE intIndex = ?";
    private static readonly string SELECT_Bind_s = "SELECT * FROM Root WHERE strIndex = ?";

    private static readonly string SELECT_Command_i = "SELECT * FROM Root ORDER BY intIndex";
    private static readonly string SELECT_Command_s = "SELECT * FROM Root ORDER BY strIndex";

    private static readonly string DELETE_Bind = "DELETE FROM Root WHERE intIndex = ?";

    private static readonly long[,] timer = new long[2, 4];

    private static string databaseName;

    public static void Main()
    {
        for (nRecords = 10000; nRecords <= 200000; nRecords *= 2)
        {
            databaseName = "Benchmark_cs-SQLite.sqlite";
            TestSQLite();
            //
            databaseName = "Benchmark_cs-Sqlite3.sqlite";
            TestCsharpSqlite();
            //
            PrintStats(nRecords);
        }

        Console.WriteLine("Enter to Continue: ");
        Console.ReadKey();
    }

    private static void TestCsharpSqlite()
    {
        SQLiteDatabase db;
        SQLiteVdbe stmt;
        SQLiteVdbe c1, c2;

        bool found;
        int i;

        var databaseName = "Benchmark_cs-SQLite.sqlite";
        if (File.Exists(databaseName)) File.Delete(databaseName);

        db = new SQLiteDatabase(databaseName);
        for (i = 0; i < PRAGMA_Commands.Length; i++) db.ExecuteNonQuery(PRAGMA_Commands[i]);

        db.ExecuteNonQuery("BEGIN EXCLUSIVE");
        for (i = 0; i < CREATE_Commands.Length; i++) db.ExecuteNonQuery(CREATE_Commands[i]);
        stmt = new SQLiteVdbe(db, INSERT_Command);
        var start = DateTime.Now.Ticks;
        long key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            stmt.Reset();
            stmt.BindLong(1, key);
            stmt.BindText(2, key.ToString());
            stmt.ExecuteStep();
        }

        stmt.Close();
        db.ExecuteNonQuery("END");
        timer[1, 0] = DateTime.Now.Ticks - start;

        db.ExecuteNonQuery("BEGIN EXCLUSIVE");
        start = DateTime.Now.Ticks;
        c1 = new SQLiteVdbe(db, SELECT_Bind_i);
        c2 = new SQLiteVdbe(db, SELECT_Bind_s);
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            c1.Reset();
            c1.BindLong(1, key);
            c1.ExecuteStep();

            c2.Reset();
            c2.BindText(1, key.ToString());
            c2.ExecuteStep();

            var id = c1.Result_Long(0);
            Debug.Assert(id == c2.Result_Long(0));
        }

        c1.Close();
        c2.Close();
        db.ExecuteNonQuery("END");
        timer[1, 1] = DateTime.Now.Ticks - start;

        db.ExecuteNonQuery("BEGIN EXCLUSIVE");
        start = DateTime.Now.Ticks;
        key = long.MinValue;
        i = 0;
        c1 = new SQLiteVdbe(db, SELECT_Command_i);
        while (c1.ExecuteStep() != Sqlite3.SQLITE_DONE)
        {
            var intKey = c1.Result_Long(0);
            Debug.Assert(intKey >= key);
            key = intKey;
            i += 1;
        }

        c1.Close();
        Debug.Assert(i == nRecords);

        var strKey = "";
        i = 0;
        c2 = new SQLiteVdbe(db, SELECT_Command_s);
        while (c2.ExecuteStep() != Sqlite3.SQLITE_DONE)
        {
            var recStrKey = c2.Result_Text(1);
            Debug.Assert(recStrKey.CompareTo(strKey) >= 0);
            strKey = recStrKey;
            i += 1;
        }

        c2.Close();
        Debug.Assert(i == nRecords);
        timer[1, 2] = DateTime.Now.Ticks - start;
        db.ExecuteNonQuery("END");

        db.ExecuteNonQuery("BEGIN EXCLUSIVE");
        start = DateTime.Now.Ticks;
        key = 1999;
        stmt = new SQLiteVdbe(db, DELETE_Bind);
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            stmt.Reset();
            stmt.BindLong(1, key);
            stmt.ExecuteStep();
        }

        stmt.Close();
        db.ExecuteNonQuery("END");
        timer[1, 3] = DateTime.Now.Ticks - start;
        db.CloseDatabase();
#if NET_35
        Sqlite3.sqlite3_shutdown();
#else
Sqlite3.sqlite3_shutdown();
#endif
    }

    private static void TestSQLite()
    {
        int i;
        var databaseName = "Benchmark_SQLite.sqlite";
        if (File.Exists(databaseName)) File.Delete(databaseName);

        var constring = new SqliteConnectionStringBuilder();
        //Commented out to compile. Check if legacy or so
        //constring.PageSize = 1024;
        //constring.SyncMode = SynchronizationModes.Off;
        constring.DataSource = databaseName;

        var con = new SqliteConnection(constring.ToString());
        con.Open();
        var com = con.CreateCommand();
        for (i = 0; i < PRAGMA_Commands.Length; i++)
        {
            com.CommandText = PRAGMA_Commands[i];
            com.ExecuteNonQuery();
        }

        for (i = 0; i < CREATE_Commands.Length; i++)
        {
            com.CommandText = CREATE_Commands[i];
            com.ExecuteNonQuery();
        }

        com.CommandText = "BEGIN EXCLUSIVE";
        com.ExecuteNonQuery();

        com.CommandText = "INSERT INTO Root VALUES (?,?)";
        var p1 = com.CreateParameter();
        p1.DbType = DbType.Int64;
        com.Parameters.Add(p1);
        var p2 = com.CreateParameter();
        p2.DbType = DbType.String;
        com.Parameters.Add(p2);

        var start = DateTime.Now.Ticks;
        long key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            p1.Value = key;
            p2.Value = key.ToString();
            com.ExecuteNonQuery();
        }

        com.CommandText = "END";
        com.Parameters.Clear();
        com.ExecuteNonQuery();
        timer[0, 0] = DateTime.Now.Ticks - start;

        com.CommandText = "BEGIN EXCLUSIVE";
        com.ExecuteNonQuery();

        using (var com2 = con.CreateCommand())
        {
            com.CommandText = SELECT_Bind_i;
            com.Parameters.Clear();
            com.Parameters.Add(p1);

            com2.CommandText = SELECT_Bind_s;
            com2.Parameters.Clear();
            com2.Parameters.Add(p2);

            start = DateTime.Now.Ticks;
            key = 1999;
            var resValues = new object[2];
            for (i = 0; i < nRecords; i++)
            {
                key = (3141592621L * key + 2718281829L) % 1000000007L;
                p1.Value = key;
                p2.Value = key.ToString();
                using (var res = com.ExecuteReader())
                {
                    res.Read();
                    res.GetValues(resValues);
                }

                var id = (long)resValues[0];
                using (var res = com2.ExecuteReader())
                {
                    res.Read();
                    res.GetValues(resValues);
                }

                Debug.Assert(id == (long)resValues[0]);
            }
        }

        timer[0, 1] = DateTime.Now.Ticks - start;
        com.CommandText = "END";
        com.Parameters.Clear();
        com.ExecuteNonQuery();

        com.CommandText = "BEGIN EXCLUSIVE";
        com.ExecuteNonQuery();

        start = DateTime.Now.Ticks;
        com.CommandText = SELECT_Command_i;
        com.Parameters.Clear();
        key = long.MinValue;
        i = 0;
        using (var reader = com.ExecuteReader())
        {
            var resValues = new object[2];
            while (reader.Read())
            {
                reader.GetValues(resValues);
                var intKey = (long)resValues[0];
                Debug.Assert(intKey >= key);
                key = intKey;
                i += 1;
            }

            Debug.Assert(i == nRecords);
        }

        com.CommandText = SELECT_Command_s;
        using (var reader = com.ExecuteReader())
        {
            i = 0;
            var strKey = "";
            var resValues = new object[2];
            while (reader.Read())
            {
                reader.GetValues(resValues);
                var recStrKey = (string)resValues[1];
                Debug.Assert(recStrKey.CompareTo(strKey) >= 0);
                strKey = recStrKey;
                i += 1;
            }

            Debug.Assert(i == nRecords);
        }

        timer[0, 2] = DateTime.Now.Ticks - start;

        com.CommandText = "END";
        com.Parameters.Clear();
        com.ExecuteNonQuery();

        com.CommandText = "BEGIN EXCLUSIVE";
        com.ExecuteNonQuery();

        com.CommandText = DELETE_Bind;
        com.Parameters.Clear();
        com.Parameters.Add(p1);

        start = DateTime.Now.Ticks;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            p1.Value = key;
            com.ExecuteNonQuery();
        }

        com.CommandText = "END";
        com.Parameters.Clear();
        com.ExecuteNonQuery();

        timer[0, 3] = DateTime.Now.Ticks - start;
        con.Close();
    }

    private static void PrintStats(int nRecords)
    {
        Console.WriteLine("          # Records Inserting Searching Iterating  Deleting");
        Console.WriteLine("   SQLite{0,10:####,###}{1,10:#####.0s}{2,10:#####.0s}{3,10:#####.0s}{4,10:#####.0s}",
            nRecords, timer[0, 0] * 10e-8 + .05, timer[0, 1] * 10e-8 + .05, timer[0, 2] * 10e-8 + .05,
            timer[0, 3] * 10e-8 + .05);
        Console.WriteLine("C#-SQLite{0,10:####,###}{1,10:#####.0s}{2,10:#####.0s}{3,10:#####.0s}{4,10:#####.0s}",
            nRecords, timer[1, 0] * 10e-8 + .05, timer[1, 1] * 10e-8 + .05, timer[1, 2] * 10e-8 + .05,
            timer[1, 3] * 10e-8 + .05);
        Console.WriteLine("C#/SQLite{0,10:####,###}{1,10:#####.0x}{2,10:#####.0x}{3,10:#####.0x}{4,10:#####.0x}",
            nRecords, (double)timer[1, 0] / timer[0, 0], (double)timer[1, 1] / timer[0, 1],
            (double)timer[1, 2] / timer[0, 2], (double)timer[1, 3] / timer[0, 3]);
    }
}