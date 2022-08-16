using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;

namespace SQLiteClientTests
{
    public class SQLiteClientTestDriver
    {
        //nSoftware code for Threading & Transactions
        private static string connstring_T6;

        private static readonly Random rnd = new Random();

        //nSoftware code for Threading
        private string connstring_T4;

        //nSoftware code for Threading
        private string connstring_T5;

        //nSoftware code for Threading
        private string connstring_T7;
        private StringBuilder TempDirectory = new StringBuilder("B:/TEMP/");

        public void Test1()
        {
            Console.WriteLine("Test1 Start.");

            Console.WriteLine("Create connection...");
            var con = new SqliteConnection();

            var dbFilename = @"SqliteTest3.db";
            var cs = string.Format("Version=3,uri=file:{0}", dbFilename);

            Console.WriteLine("Set connection String: {0}", cs);

            if (File.Exists(dbFilename))
                File.Delete(dbFilename);

            con.ConnectionString = cs;

            Console.WriteLine("Open database...");
            con.Open();

            Console.WriteLine("create command...");
            IDbCommand cmd = con.CreateCommand();

            Console.WriteLine("create table TEST_TABLE...");
            cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 1...");
            cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 2...");
            cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
            cmd.ExecuteNonQuery();

            //Console.WriteLine("commit...");
            //cmd.CommandText = "COMMIT";
            //cmd.ExecuteNonQuery();

            Console.WriteLine("SELECT data from TEST_TABLE...");
            cmd.CommandText = "SELECT COLA, COLB, COLC FROM TEST_TABLE";
            var reader = cmd.ExecuteReader();
            var r = 0;
            Console.WriteLine("Read the data...");
            while (reader.Read())
            {
                Console.WriteLine("  Row: {0}", r);
                var i = reader.GetInt32(reader.GetOrdinal("COLA"));
                Console.WriteLine("    COLA: {0}", i);

                var s = reader.GetString(reader.GetOrdinal("COLB"));
                Console.WriteLine("    COLB: {0}", s);

                var dt = reader.GetDateTime(reader.GetOrdinal("COLC"));
                Console.WriteLine("    COLB: {0}", dt.ToString("MM/dd/yyyy HH:mm:ss"));

                r++;
            }

            Console.WriteLine("Rows retrieved: {0}", r);

//alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT
            var command = new SqliteCommand("PRAGMA table_info('TEST_TABLE')", con);
            var dataTable = new DataTable();
            var dataAdapter = new SqliteDataAdapter();
            dataAdapter.SelectCommand = command;
            dataAdapter.Fill(dataTable);
            DisplayDataTable(dataTable, "Columns");
#endif

            Console.WriteLine("Close and cleanup...");
            con.Close();
            con = null;

            Console.WriteLine("Test1 Done.");
        }

        public void Test2()
        {
            Console.WriteLine("Test2 Start.");

            Console.WriteLine("Create connection...");
            var con = new SqliteConnection();

            var dbFilename = @"SqliteTest3.db";
            var cs = string.Format("Version=3,uri=file:{0}", dbFilename);

            Console.WriteLine("Set connection String: {0}", cs);

            if (File.Exists(dbFilename))
                File.Delete(dbFilename);

            con.ConnectionString = cs;

            Console.WriteLine("Open database...");
            con.Open();

            Console.WriteLine("create command...");
            IDbCommand cmd = con.CreateCommand();

            Console.WriteLine("create table TEST_TABLE...");
            cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, NAME TEXT)";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 1...");
            cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (1, 'ONE' )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 2...");
            cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (2, 'ä¸­æ–‡' )";
            cmd.ExecuteNonQuery();

            //Console.WriteLine("commit...");
            //cmd.CommandText = "COMMIT";
            //cmd.ExecuteNonQuery();

            Console.WriteLine("SELECT data from TBL...");
            cmd.CommandText = "SELECT id,NAME FROM tbl WHERE name = 'ä¸­æ–‡'";
            var reader = cmd.ExecuteReader();
            var r = 0;
            Console.WriteLine("Read the data...");
            while (reader.Read())
            {
                Console.WriteLine("  Row: {0}", r);
                var i = reader.GetInt32(reader.GetOrdinal("ID"));
                Console.WriteLine("    ID: {0}", i);

                var s = reader.GetString(reader.GetOrdinal("NAME"));
                Console.WriteLine("    NAME: {0} = {1}", s, s == "ä¸­æ–‡");
                r++;
            }

            Console.WriteLine("Rows retrieved: {0}", r);

            //alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT
            var command = new SqliteCommand("PRAGMA table_info('TEST_TABLE')", con);
            var dataTable = new DataTable();
            var dataAdapter = new SqliteDataAdapter();
            dataAdapter.SelectCommand = command;
            dataAdapter.Fill(dataTable);
            DisplayDataTable(dataTable, "Columns");
#endif

            Console.WriteLine("Close and cleanup...");
            con.Close();
            con = null;

            Console.WriteLine("Test1 Done.");
        }

        public void Test3()
        {
            Console.WriteLine("Test3 (Date Paramaters) Start.");

            Console.WriteLine("Create connection...");
            var con = new SqliteConnection();

            var dbFilename = @"SqliteTest3.db";
            var cs = string.Format("Version=3,uri=file:{0}", dbFilename);

            Console.WriteLine("Set connection String: {0}", cs);

            if (File.Exists(dbFilename))
                File.Delete(dbFilename);

            con.ConnectionString = cs;

            Console.WriteLine("Open database...");
            con.Open();

            Console.WriteLine("create command...");
            IDbCommand cmd = con.CreateCommand();

            Console.WriteLine("create table TEST_TABLE...");
            cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, DATE_TEXT REAL)";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert ...");
            cmd.CommandText = "INSERT INTO TBL  ( ID, DATE_TEXT) VALUES ( 1,  @DATETEXT)";
            cmd.Parameters.Add(
                new SqliteParameter
                {
                    ParameterName = "@DATETEXT",
                    Value = DateTime.Now
                }
            );

            cmd.ExecuteNonQuery();


            Console.WriteLine("SELECT data from TBL...");
            cmd.CommandText = "SELECT * FROM tbl";
            var reader = cmd.ExecuteReader();
            var r = 0;
            Console.WriteLine("Read the data...");
            while (reader.Read())
            {
                Console.WriteLine("  Row: {0}", r);
                var i = reader.GetInt32(reader.GetOrdinal("ID"));
                Console.WriteLine("    ID: {0}", i);

                var s = reader.GetString(reader.GetOrdinal("DATE_TEXT"));
                Console.WriteLine("    DATE_TEXT: {0}", s);
                r++;
            }

            Console.WriteLine("Rows retrieved: {0}", r);


            Console.WriteLine("Close and cleanup...");
            con.Close();
            con = null;

            Console.WriteLine("Test3 Done.");
        }

        public void Test4()
        {
            var dbFilename = "threading_t4.db";
            if (File.Exists(dbFilename))
                File.Delete(dbFilename);
            connstring_T4 = @"Version=3,busy_timeout=100,uri=file:" + dbFilename;

            Setup_T4();
            InsertSameTable_T4(); //concurrent inserts
            SelectorWrite_T4(); //concurrent selects and inserts
            Console.WriteLine("Testing for Threading done. Press enter to continue");
            Console.In.Read();
        }

        private void SelectorWrite_T4()
        {
            //concurrent reads/writes in the same table, if there were only Selects it would be preferable for the sqlite engine not to lock internally.
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("SELECT/INSERT ON Thread {0}", i);
                var worker = new Thread(() =>
                {
                    // Cannot use value of i, since it exceeds the scope of this thread and will be 
                    // reused by multiple threads
                    var aValue = 100 + Thread.CurrentThread.ManagedThreadId;
                    var op = aValue % 2;

                    var con = new SqliteConnection();
                    con.ConnectionString = connstring_T4;
                    con.Open();
                    IDbCommand cmd = con.CreateCommand();
                    cmd = con.CreateCommand();
                    if (op == 0)
                    {
                        cmd.CommandText = "Select * FROM ATABLE";
                        cmd.ExecuteReader();
                    }
                    else
                    {
                        cmd.CommandText = string.Format("INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )",
                            aValue);
                        Console.WriteLine(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                });
                worker.Start();
            }
        }

        //we need concurrency support on a table level inside of the database file.
        private void InsertSameTable_T4()
        {
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("INSERTING ON Thread {0}", i);
                var worker = new Thread(() =>
                    {
                        // Cannot use value of i, since it exceeds the scope of this thread and will be 
                        // reused by multiple threads

                        var aValue = Thread.CurrentThread.ManagedThreadId;

                        var con = new SqliteConnection();
                        con.ConnectionString = connstring_T4;
                        con.Open();
                        IDbCommand cmd = con.CreateCommand();
                        cmd = con.CreateCommand();
                        cmd.CommandText = string.Format("INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )",
                            aValue);
                        Console.WriteLine(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                );
                worker.Start();
            }
        }

        private void Setup_T4()
        {
            var con = new SqliteConnection();
            con.ConnectionString = connstring_T4;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )";
            cmd.ExecuteNonQuery();
        }

        public void Test5()
        {
            var dbFilename = "threading_t5.db";
            if (File.Exists(dbFilename))
                File.Delete(dbFilename);
            connstring_T5 = @"Version=3,busy_timeout=2500,uri=file:" + dbFilename;

            Setup_T5();
            MultiInsertsSameThread_T5(); //concurrent inserts
            Console.WriteLine("Threads are running...");
            Console.In.Read();
        }

        private void MultiInsertsSameThread_T5()
        {
            for (var i = 0; i < 10; i++)
            {
                //Console.WriteLine( "SELECT/INSERT ON Thread {0}", i );
                var worker = new Thread(() =>
                {
                    var commandt = string.Empty;
                    try
                    {
                        // Cannot use value of i, since it exceeds the scope of this thread and will be 
                        // reused by multiple threads
                        var aValue = 100 + Thread.CurrentThread.ManagedThreadId;
                        var op = aValue % 2;

                        var con = new SqliteConnection();
                        con.ConnectionString = connstring_T5;
                        con.Open();
                        IDbCommand cmd = con.CreateCommand();
                        cmd = con.CreateCommand();
                        if (op == 0)
                        {
                            for (var j = 0; j < 1000; j++)
                            {
                                int rows;
                                var retry = 0;
                                cmd.CommandText =
                                    string.Format("INSERT INTO BTABLE ( A, B, C ) VALUES ({0},'threader', '1' )",
                                        aValue * 10000 + j);
                                commandt = cmd.CommandText;
                                do
                                {
                                    rows = cmd.ExecuteNonQuery();
                                    if (rows == 0)
                                    {
                                        retry += 1; // Insert Failed
                                        Console.WriteLine(cmd.CommandText);
                                        Console.WriteLine("retry {0}", retry);
                                        Console.WriteLine(((SqliteCommand)cmd).GetLastError());
                                    }
                                } while (rows == 0 && retry < 5);
                            }
                        }
                        else
                        {
                            cmd.CommandText = "Select * FROM ATABLE";
                            commandt = cmd.CommandText;
                            cmd.ExecuteReader();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Command {0} threw exception {1}", commandt, ex.Message);
                    }
                });

                worker.Start();
            }
        }

        private void Setup_T5()
        {
            var con = new SqliteConnection();
            con.ConnectionString = connstring_T5;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )";
            cmd.ExecuteNonQuery();
        }

        public void Test6()
        {
            var dbFilename = "threading_t6.db";
            if (File.Exists(dbFilename))
                File.Delete(dbFilename);
            connstring_T6 = @"Version=3,busy_timeout=2000,uri=file:" + dbFilename;

            Setup_T6();
            MultiInsertsTransactionsSameThread_T6(); //concurrent inserts
            Console.WriteLine("Threads are running...");
            Console.In.Read();
        }

        private void MultiInsertsTransactionsSameThread_T6()
        {
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(rnd.Next(100, 1000));
                Console.WriteLine("Launching Thread {0}", i);
                var worker = new Thread(T6_ThreadStart);
                worker.Start(i);
            }
        }

        private static void T6_ThreadStart(object data)
        {
            var commandt = string.Empty;
            var i = (int)data;
            try
            {
                var aValue = 100 + i;
                var op = aValue % 2;

                var con = new SqliteConnection();
                con.ConnectionString = connstring_T6;
                con.Open();
                IDbCommand cmd = con.CreateCommand();
                cmd = con.CreateCommand();
                if (op == 0)
                {
                    var trans = (SqliteTransaction)con.BeginTransaction();
                    for (var j = 0; j < 5000; j++)
                    {
                        int rows;
                        var retry = 0;
                        cmd.CommandText = string.Format("INSERT INTO BTABLE ( A, B, C ) VALUES ({0},'threader', '1' )",
                            aValue * 10000 + j);
                        commandt = cmd.CommandText;
                        do
                        {
                            rows = cmd.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                retry += 1; // Insert Failed
                                Console.WriteLine("retry {0}:{1}:{2}", retry, ((SqliteCommand)cmd).GetLastError(),
                                    cmd.CommandText);
                                Thread.Sleep(rnd.Next(50, 1000));
                            }
                        } while (rows == 0 && retry < 10);
                    }

                    trans.Commit();
                }
                else
                {
                    cmd.CommandText = "Select * FROM ATABLE";
                    commandt = cmd.CommandText;
                    cmd.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Command {0} threw exception {1}", commandt, ex.Message);
            }
        }

        private void Setup_T6()
        {
            var con = new SqliteConnection();
            con.ConnectionString = connstring_T6;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )";
            cmd.ExecuteNonQuery();
        }

        public void Test7()
        {
            var dbFilename = "threading_t7.db";
            if (File.Exists(dbFilename))
                File.Delete(dbFilename);
            connstring_T7 = @"Version=3,busy_timeout=1000,uri=file:" + dbFilename;

            MultiInsertsSameThread_T7(); //concurrent table creation
            Console.WriteLine("Threads are running...");
            Console.In.Read();
        }

        private void MultiInsertsSameThread_T7()
        {
            var lthread = new List<Thread>();
            for (var i = 0; i < 70; i++)
            {
                Thread.Sleep(rnd.Next(100, 1000));
                Console.WriteLine("Launching Thread {0}", i);
                var worker = new Thread(T7_ThreadStart);
                lthread.Add(worker);
                worker.Start(i);
            }

            var alldone = false;

            while (!alldone)
            {
                alldone = true;
                for (var i = 0; i < lthread.Count; i++)
                {
                    if (lthread[i].ThreadState == ThreadState.Running)
                        alldone = false;
                    Thread.Sleep(100);
                }
            }

            Console.WriteLine("finished");
        }

        private void T7_ThreadStart(object iSequence)
        {
            var aValue = (int)iSequence * 1000;
            var op = aValue % 2;
            var rows = 0;

            var con = new SqliteConnection();
            con.ConnectionString = connstring_T7;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            var commandt =
                string.Format(
                    "CREATE TABLE IF NOT EXISTS ATABLE{0}(A integer primary key , B varchar (50), C integer, D varchar (500))",
                    aValue);
            cmd.CommandText = commandt;
            try
            {
                rows = cmd.ExecuteNonQuery();
                Console.WriteLine("Created table: ATABLE" + aValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Command {0} threw exception {1}", commandt, ex.Message);
            }
        }

        private void Setup_T7()
        {
            var con = new SqliteConnection();
            con.ConnectionString = connstring_T7;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )";
            cmd.ExecuteNonQuery();
        }


        public void Issue_65()
        {
            //alxwest: causes error "Unable to open database" as TempDirectory.ToString() is set to "B:/TEMP/"
            //string datasource = "file://" + TempDirectory.ToString() + "myBigDb.s3db";
            var datasource = "file://" + "myBigDb.s3db";

            using (IDbConnection conn = new SqliteConnection("uri=" + datasource))
            {
                var targetFileSize = (long)Math.Pow(2, 32) - 1;
                var rowLength = 1024; // 2^10

                long loopCount = (int)(targetFileSize / rowLength) + 10000;

                var chars = new char[rowLength];
                for (var i = 0; i < rowLength; i++) chars[i] = 'A';

                var row = new string(chars);

                conn.Open();
                var cmd = conn.CreateCommand();

                try
                {
                    cmd.CommandText =
                        "PRAGMA cache_size = 16000; PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "drop table if exists [MyTable]";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "create table [MyTable] ([MyField] varchar(" + rowLength + ") null)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "insert into [MyTable] ([MyField]) VALUES ('" + row + "')";
                    for (var i = 0; i < loopCount; i++) cmd.ExecuteNonQuery();
                }
                catch
                {
                    Console.WriteLine(((SqliteCommand)cmd).GetLastError());
                }
                finally
                {
                    cmd.Cancel();
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        //Issue 76 Encryption is not implemented in C#SQLite client connection and command objects 
        public void Issue_76()
        {
            Console.WriteLine("Test for Issue_76 Start.");

            Console.WriteLine("Create connection...");
            var con = new SqliteConnection();

            var dbFilename = @"SqliteTest3.db";
            var cs = string.Format("Version=3,uri=file:{0}", dbFilename);

            Console.WriteLine("Set connection String: {0}", cs);

            if (File.Exists(dbFilename))
                File.Delete(dbFilename);

            con.ConnectionString = cs;

            Console.WriteLine("Open database...");
            con.Open();

            Console.WriteLine("create command...");
            IDbCommand cmd = con.CreateCommand();

            cmd.CommandText = "pragma hexkey='0x73656372657470617373776F72640f11'";
            Console.WriteLine(cmd.CommandText);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "create table a (b); insert into a values ('row 1');select * from a;";
            Console.WriteLine(cmd.CommandText);
            Console.WriteLine("Result {0}", cmd.ExecuteScalar());

            Console.WriteLine("Close & Reopen Connection");
            con.Close();
            con.Open();

            cmd.CommandText = "select * from a;";
            Console.WriteLine(cmd.CommandText);
            Console.WriteLine("Result {0}", cmd.ExecuteScalar());

            Console.WriteLine("Close & Reopen Connection");
            con.Close();
            con.Open();
            cmd.CommandText = "pragma hexkey='0x73656372657470617373776F72640f11'";
            Console.WriteLine(cmd.CommandText);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "select * from a;";
            Console.WriteLine(cmd.CommandText);
            Console.WriteLine("Result {0}", cmd.ExecuteScalar());

            Console.WriteLine("Close & Reopen Connection with password");
            con.Close();

            con.ConnectionString = cs + ",Password=0x73656372657470617373776F72640f11";
            con.Open();
            cmd.CommandText = "select * from a;";
            Console.WriteLine(cmd.CommandText);
            Console.WriteLine("Result {0}", cmd.ExecuteScalar());

            con = null;

            Console.WriteLine("Issue_76 Done.");
        }

        //Multi thread execution special command or ddl command results in exception..
        public void Issue_86()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, eventArgs) => { Console.WriteLine(eventArgs.ExceptionObject); };
            var flags = Sqlite3.SQLITE_OPEN_NOMUTEX | Sqlite3.SQLITE_OPEN_READWRITE | Sqlite3.SQLITE_OPEN_CREATE;
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("Running Thread {0}", i);
                var t = new Thread(
                    () =>
                    {
                        var dbFilename = string.Format("db{0}.sqlite", Thread.CurrentThread.ManagedThreadId);
                        if (File.Exists(dbFilename))
                            File.Delete(dbFilename);
                        Console.WriteLine("Using Database {0}", dbFilename);
                        Sqlite3.sqlite3 db = null;
                        Sqlite3.sqlite3_open_v2(dbFilename, out db, flags, null);
                        var command = string.Format("create table [t{0}] (id, name, amount)",
                            Thread.CurrentThread.ManagedThreadId);
                        ExecuteCommand(db, command);
                        Sqlite3.sqlite3_close(db);
                    });
                t.Start();
            }
        }

        private static void ExecuteCommand(Sqlite3.sqlite3 db, string command)
        {
            int rc;
            Sqlite3.Vdbe vm = null;
            if (Sqlite3.sqlite3_prepare_v2(db, command, command.Length, ref vm, 0) != Sqlite3.SQLITE_OK)
                throw new InvalidOperationException(string.Format("Query failed ({0}), message: {1}.", db.errCode,
                    Sqlite3.sqlite3_errmsg(db)));
            rc = Sqlite3.sqlite3_step(vm);
            if (rc != Sqlite3.SQLITE_DONE && rc != Sqlite3.SQLITE_ROW)
                throw new InvalidOperationException(string.Format("Query failed ({0}), message: {1}.", db.errCode,
                    Sqlite3.sqlite3_errmsg(db)));
            Sqlite3.sqlite3_finalize(vm);
        }

//alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT
        public void DisplayDataTable(DataTable table, string name)
        {
            Console.WriteLine("Display DataTable: {0}", name);
            var r = 0;
            foreach (DataRow row in table.Rows)
            {
                Console.WriteLine("Row {0}", r);
                var c = 0;
                foreach (DataColumn col in table.Columns)
                {
                    Console.WriteLine("   Col {0}: {1} {2}", c, col.ColumnName, col.DataType);
                    Console.WriteLine("       Value: {0}", row[col]);
                    c++;
                }

                r++;
            }

            Console.WriteLine("Rows in data table: {0}", r);
        }
#endif


        public void Issue_119()
        {
            Console.WriteLine("Test Start.");

            Console.WriteLine("Create connection...");
            var con = new SqliteConnection();

            var dbFilename = @"=SqliteTest3=.db";
            var cs = string.Format("Version=3,uri=file:{0}", dbFilename);

            Console.WriteLine("Set connection String: {0}", cs);

            if (File.Exists(dbFilename))
                File.Delete(dbFilename);

            con.ConnectionString = cs;

            Console.WriteLine("Open database...");
            con.Open();

            Console.WriteLine("create command...");
            IDbCommand cmd = con.CreateCommand();

            Console.WriteLine("create table TEST_TABLE...");
            cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 1...");
            cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("insert row 2...");
            cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
            cmd.ExecuteNonQuery();

            Console.WriteLine("SELECT data from TEST_TABLE...");
            cmd.CommandText = "SELECT RowID, COLA, COLB, COLC FROM TEST_TABLE";
            var reader = cmd.ExecuteReader();
            var r = 0;
            Console.WriteLine("Read the data...");
            while (reader.Read())
            {
                Console.WriteLine("  Row: {0}", r);
                var rowid = reader.GetInt32(reader.GetOrdinal("RowID"));
                Console.WriteLine("    RowID: {0}", rowid);

                var i = reader.GetInt32(reader.GetOrdinal("COLA"));
                Console.WriteLine("    COLA: {0}", i);

                var s = reader.GetString(reader.GetOrdinal("COLB"));
                Console.WriteLine("    COLB: {0}", s);

                var dt = reader.GetDateTime(reader.GetOrdinal("COLC"));
                Console.WriteLine("    COLB: {0}", dt.ToString("MM/dd/yyyy HH:mm:ss"));

                r++;
            }

            Console.WriteLine("Close and cleanup...");
            con.Close();
            con = null;

            Console.WriteLine("Test Done.");
        }

        public void Issue_124()
        {
            Console.WriteLine("Test Start.");

            Sqlite3.sqlite3 db = null;
            Sqlite3.sqlite3_open(":memory:", out db);
            Sqlite3.Vdbe stmt = null;
            string zero = null;
            string val;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            //create table
            {
                Sqlite3.sqlite3_prepare_v2(db, "create table Test (val REAL NOT NULL)", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                Sqlite3.sqlite3_finalize(stmt);
            }

            //insert 0.1
            {
                Sqlite3.sqlite3_prepare_v2(db, "insert into Test(val) values ('0.1')", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                Sqlite3.sqlite3_finalize(stmt);
            }
            //insert 0.1
            {
                Sqlite3.sqlite3_prepare_v2(db, "insert into Test(val) values ('0.2')", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                Sqlite3.sqlite3_finalize(stmt);
            }

            //insert 0.000000001
            {
                Sqlite3.sqlite3_prepare_v2(db, "insert into Test(val) values ('0.000000001')", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                Sqlite3.sqlite3_finalize(stmt);
            }

            //invariant culture
            {
                Console.WriteLine("invariant culture");
                Sqlite3.sqlite3_prepare_v2(db, "select val from Test", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                val = Sqlite3.sqlite3_column_text(stmt, 0);
                Console.WriteLine("value: " + val);
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru");
                Sqlite3.sqlite3_step(stmt);
                val = Sqlite3.sqlite3_column_text(stmt, 0);
                Console.WriteLine("value: " + val);
                Sqlite3.sqlite3_step(stmt);
                val = Sqlite3.sqlite3_column_text(stmt, 0);
                Console.WriteLine("value: " + val);
                Sqlite3.sqlite3_finalize(stmt);
            }

            //ru-ru culture
            {
                Console.WriteLine("ru");
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru");
                Sqlite3.sqlite3_prepare_v2(db, "select val from Test", -1, ref stmt, ref zero);
                Sqlite3.sqlite3_step(stmt);
                val = Sqlite3.sqlite3_column_text(stmt, 0);
                Console.WriteLine("value: " + val);
                Sqlite3.sqlite3_step(stmt);
                val = Sqlite3.sqlite3_column_text(stmt, 0);
                Console.WriteLine("value: " + val);
                Sqlite3.sqlite3_finalize(stmt);
            }


            Console.WriteLine("Test Done.");
        }

        public static int Main(string[] args)
        {
            var tests = new SQLiteClientTestDriver();

            var Test = 124;
            switch (Test)
            {
                case 1:
                    tests.Test1();
                    break;
                case 2:
                    tests.Test2();
                    break;
                case 3:
                    tests.Test3();
                    break;
                case 4:
                    tests.Test4();
                    break;
                case 5:
                    tests.Test5();
                    break;
                case 6:
                    tests.Test6();
                    break;
                case 7:
                    tests.Test7();
                    break;
                case 65:
                    tests.Issue_65();
                    break;
                case 76:
                    tests.Issue_76();
                    break;
                case 86:
                    tests.Issue_86();
                    break;
                case 119:
                    tests.Issue_119();
                    break;
                case 124:
                    tests.Issue_124();
                    break;
            }

            Console.WriteLine("Press Enter to Continue");
            Console.ReadKey();
            tests = null;

            return 0;
        }
    }
}