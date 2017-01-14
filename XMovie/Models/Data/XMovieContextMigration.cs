using Microsoft.Practices.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SQLite;
using XMovie.Common;

namespace XMovie.Models.Data
{
    public class XMovieContextMigration
    {
        public static int RequiredDatabaseVersion = 1;

        private Dictionary<int, List<string>> migrations;

        public XMovieContextMigration()
        {
            migrations = new Dictionary<int, List<string>>();

            MigrationVersion1();
        }

        public void Migration()
        {
            var logger = App.Container.Resolve<Logger>();

            string connectionString;
            using (var context = new XMovieContext())
            {
                connectionString = context.Database.Connection.ConnectionString;
            }

            var currentVersion = 0;
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "pragma user_version";
                    currentVersion = int.Parse(cmd.ExecuteScalar().ToString());
                }
            }

            if (currentVersion == RequiredDatabaseVersion)
            {
                logger.Information($"DBバージョンは最新です。Version {currentVersion}");
                return;
            }

            using (var context = new XMovieContext())
            {
                while (currentVersion < RequiredDatabaseVersion)
                {
                    currentVersion++;
                    logger.Information($"DBマイグレーション: Version {currentVersion - 1} -> {currentVersion}");

                    foreach (var sql in migrations[currentVersion])
                    {
                        context.Database.ExecuteSqlCommand(sql);
                    }
                }
                context.Database.ExecuteSqlCommand($"pragma user_version = {currentVersion}");
            }
        }

        private void MigrationVersion1()
        {
            var step = new List<string>();

            step.Add("alter table Movies add column Title text default null");

            migrations.Add(1, step); 
        }
    }

    public class UserVersion
    {
        [Column("user_version")]
        public int Version { get; set; }
    }
}
