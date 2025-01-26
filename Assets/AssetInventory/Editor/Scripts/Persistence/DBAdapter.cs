using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;

namespace AssetInventory
{
    public static class DBAdapter
    {
        public const string DB_NAME = "AssetInventory.db";

        public static SQLiteConnection DB
        {
            get
            {
                if (_db == null) InitDB();
                return _db;
            }
        }

        private static SQLiteConnection _db;

        public static void InitDB()
        {
            _db = new SQLiteConnection(GetDBPath());
            _db.EnableWriteAheadLogging();
            _db.CreateTable<Asset>();
            _db.CreateTable<AssetFile>();
            _db.CreateTable<AssetMedia>();
            _db.CreateTable<AppProperty>();
            _db.CreateTable<Tag>();
            _db.CreateTable<TagAssignment>();
            _db.CreateTable<RelativeLocation>();
            _db.CreateTable<SystemData>();
        }

        public static long GetDBSize()
        {
            return new FileInfo(GetDBPath()).Length;
        }

        public static bool ColumnExists(string tableName, string columnName)
        {
            List<SQLiteConnection.ColumnInfo> cols = DB.GetTableInfo(tableName);
            return cols.Any(c => c.Name == columnName);
        }

        public static long Compact()
        {
            long original = new FileInfo(GetDBPath()).Length;

            DB.Execute("vacuum;");

            return original - new FileInfo(GetDBPath()).Length;
        }

        public static string GetDBPath()
        {
            return IOUtils.PathCombine(AssetInventory.GetStorageFolder(), DB_NAME);
        }

        public static bool IsDBOpen()
        {
            return _db != null;
        }

        public static void Close()
        {
            if (_db == null) return;
            _db.Close();
            _db = null;
        }

        public static bool DeleteDB()
        {
            if (IsDBOpen()) Close();
            try
            {
                File.Delete(GetDBPath());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}