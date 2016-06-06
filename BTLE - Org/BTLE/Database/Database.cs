using BTLE.Misc;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedVariable
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedMember.Global

namespace BTLE.Database
{
    internal static class Database
    {
        private static string _dbPath = string.Empty;

        public static string DbPath
        {
            get
            {
                if (string.IsNullOrEmpty(_dbPath))
                {
                    //_dbPath = Path.Combine( ApplicationData.Current.LocalFolder.Path, "Storage.sqlite" );
                    _dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "UP.sqlite");
                }

                return _dbPath;
            }
        }

        private static SQLiteConnection DbConnection => new SQLiteConnection(new SQLitePlatformWinRT(), DbPath);

        /// <summary>
        /// Create the database for TickRecords
        /// </summary>
        public static void CreateDatabaseForTickRecords()
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                var c = db.CreateTable<TickRecord>();
                var info = db.GetMapping(typeof(TickRecord));
            }
        }

        /// <summary>
        /// Delete the database
        /// </summary>
        public static void DeleteDatabase()
        {
            if (File.Exists(DbPath))
            {
                using (var db = DbConnection)
                {
                    DbConnection.Close();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    File.Delete(DbPath);
                }
            }
        }

        /// <summary>
        /// Retrive all the TickRecords in the Database
        /// </summary>
        /// <returns></returns>

        #region TickRecords

        public static ObservableCollection<TickRecord> GetAllTickRecords()
        {
            ObservableCollection<TickRecord> list;

            // Create a new connection
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                list = new ObservableCollection<TickRecord>(db.Table<TickRecord>().Select(i => i));
            }

            return list;
        }

        /// <summary>
        /// Retrieve a TickRecord by Id
        /// </summary>
        /// <param name="key">TickRecord Id</param>
        /// <returns>TickRecord</returns>
        public static bool DoesRecordExist(long key)
        {
            return SearchTickRecordByKey(key) != null;
        }

        /// <summary>
        /// Retrieve a TickRecord by Id
        /// </summary>
        /// <param name="key">TickRecord Id</param>
        /// <returns>TickRecord</returns>
        public static TickRecord SearchTickRecordByKey(long key)
        {
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                TickRecord m = (from p in db.Table<TickRecord>()
                                where p.Key == key
                                select p).FirstOrDefault();
                return m;
            }
        }

        /// <summary>
        /// With a TickRecord Id == 0, will create a new TickRecord
        /// </summary>
        /// <param name="tickRecords"></param>
        public static void AddOrUpdateTickRecords(ObservableCollection<TickRecord> tickRecords)
        {
            // Create a new connection
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                db.BeginTransaction();
                try
                {
                    foreach (var tickRecord in tickRecords)
                    {
                        if (DoesRecordExist(tickRecord.Key) == false)
                        {
                            // New
                            db.Insert(tickRecord);
                        }
                        else
                        {
                            // Update
                            db.Update(tickRecord);
                        }
                    }

                    db.Commit();
                }
                catch (Exception)
                {
                    db.Rollback();
                }
            }
        }

        /// <summary>
        /// With a TickRecord Id == 0, will create a new TickRecord
        /// </summary>
        /// <param name="tickRecord"></param>
        public static void AddTickRecord(TickRecord tickRecord)
        {
            // Create a new connection
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                // New
                db.Insert(tickRecord);
            }
        }

        /// <summary>
        /// With a TickRecord Id == 0, will create a new TickRecord
        /// </summary>
        /// <param name="tickRecord"></param>
        public static void UpdateTickRecord(TickRecord tickRecord)
        {
            // Create a new connection
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                // Update
                db.Update(tickRecord);
            }
        }

        /// <summary>
        /// With a TickRecord Id == 0, will create a new TickRecord
        /// </summary>
        /// <param name="tickRecord"></param>
        public static void AddOrUpdateTickRecord(TickRecord tickRecord)
        {
            // Create a new connection
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                if (DoesRecordExist(tickRecord.Key) == false)
                {
                    // New
                    db.Insert(tickRecord);
                }
                else
                {
                    // Update
                    db.Update(tickRecord);
                }
            }
        }

        /// <summary>
        /// Add many TickRecords
        /// </summary>
        /// <param name="list"></param>
        public static void DeleteDuplicates(IEnumerable<TickRecord> list)
        {
            foreach (var item in list)
            {
                if (DoesRecordExist(item.Key))
                {
                    DeleteTickRecordById(item.Key);
                }
            }
        }

        /// <summary>
        /// Add many TickRecords
        /// </summary>
        /// <param name="list"></param>
        public static void AddAllTickRecords(IEnumerable<TickRecord> list)
        {
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                //TickRecord[ ] tickRecords = list as TickRecord[ ] ?? list.ToArray();

                //DeleteDuplicates( tickRecords );
                //db.InsertAll( tickRecords );

                db.InsertOrReplaceAll(list);
            }
        }

        /// <summary>
        /// Delete the database content
        /// </summary>
        public static void DeleteAllDatabaseTables(bool recreate = false)
        {
            // This will remove all the data and will reset the Primary Key
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                // Option 1 - SQL Syntax:
                db.Execute("DROP TABLE IF EXISTS TickRecord");
                //db.Execute( "DROP TABLE IF EXISTS Grade" );

                if (recreate)
                {
                    // ReCreate the Tables
                    db.CreateTable<TickRecord>();
                }

                // Option 2
                //db.CreateTable<TickRecord>();

                //db.DropTable<Grade>();
                //db.CreateTable<Grade>();
            }
        }

        public static void ClearAllDatabase()
        {
            // This will remove all the data but will not reset the Primary Key
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                // http://stackoverflow.com/questions/1601697/sqlite-reset-primary-id-field
                // Option 1 - SQL Syntax:
                db.Execute("DELETE FROM TickRecord");

                // This will reset the PrimaryKey to 1
                db.Execute("UPDATE SQLITE_SEQUENCE SET SEQ = 0 WHERE NAME = 'TickRecord'");

                //db.Execute( "DELETE FROM Grade" );

                // This will reset the PrimaryKey to 1
                //db.Execute( "UPDATE SQLITE_SEQUENCE SET SEQ = 0 WHERE NAME = 'Grade'" );

                // Option 2
                //db.DeleteAll<TickRecord>();
                //db.DeleteAll<Grade>();
            }
        }

        /// <summary>
        /// Delete all TickRecords in the database
        /// </summary>
        /// <param name="tickRecords"></param>
        public static void DeleteTickRecords(IEnumerable<TickRecord> tickRecords)
        {
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                foreach (var item in tickRecords)
                {
                    db.Delete(item);
                }
            }
        }

        /// <summary>
        /// Delete a TickRecord by id
        /// </summary>
        /// <param name="key"></param>
        public static void DeleteTickRecordById(long key)
        {
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                TickRecord m = (from p in db.Table<TickRecord>()
                                where p.Key == key
                                select p).FirstOrDefault();
                if (m != null)
                {
                    db.Delete(m);
                }
            }
        }

        /// <summary>
        /// Get the number of TickRecords in the database
        /// </summary>
        /// <returns></returns>
        public static int CountTickRecords()
        {
            int count;
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                count = db.Table<TickRecord>().Count();

                //count = ( from p in db.Table<TickRecord>()
                //          select p ).Count();
            }

            return count;
        }

        /// <summary>
        /// Get the number of TickRecords in the database
        /// </summary>
        /// <returns></returns>
        public static int CountSteps(DateTime start, DateTime end)
        {
            //ObservableCollection<TickRecord> records = GetAllTickRecords();
            //return records.Where( record => record.TickStartDate.LocalDateTime >= start && record.TickEndDate.LocalDateTime <= end ).Sum( record => record.PostProcessedStepCount );

            int steps;
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                steps = (from p in db.Table<TickRecord>()
                         where p.TickStartDate >= start && p.TickEndDate <= end
                         select p.PostProcessedStepCount).Sum();
            }

            return steps;
        }

        /// <summary>
        /// Get the number of TickRecords in the database
        /// </summary>
        /// <returns></returns>
        public static double SumDistance(DateTime start, DateTime end)
        {
            //ObservableCollection<TickRecord> records = GetAllTickRecords();
            //return records.Where( record => record.TickStartDate.LocalDateTime >= start && record.TickEndDate.LocalDateTime <= end ).Sum( record => record.DistanceInMeters );

            double distanceInMeters;
            using (var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath))
            {
                distanceInMeters = (from p in db.Table<TickRecord>()
                                    where p.TickStartDate >= start && p.TickEndDate <= end
                                    select p.DistanceInMeters).Sum();
            }

            return distanceInMeters;
        }

        #endregion TickRecords
    }
}