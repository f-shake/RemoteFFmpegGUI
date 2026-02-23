using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SimpleFFmpegGUI.Model
{
    public class FFmpegDbContext : DbContext
    {
        public FFmpegDbContext(DbContextOptions<FFmpegDbContext> options) : base(options)
        {
        }

        private const string CurrentVersion = "20230408";

        public FFmpegDbContext()
        {
        }

        public DbSet<Config> Configs { get; set; }

        public DbSet<Log> Logs { get; set; }

        public DbSet<CodePreset> Presets { get; set; }

        public DbSet<TaskInfo> Tasks { get; set; }

        public void Check()
        {
            var changed = false;
            foreach (var item in Tasks.Where(p => p.Status == TaskStatus.Processing))
            {
                changed = true;
                item.Status = TaskStatus.Error;
                item.Message = "状态异常：启动时处于正在运行状态";
            }

            if (changed)
            {
                SaveChanges();
            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //对于非结构化数据，采用Json的方式进行存储
            var listConverter = new EFJsonConverter<List<InputArguments>>();
            var argConverter = new EFJsonConverter<OutputArguments>();
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TaskInfo>()
                .Property(p => p.Inputs)
                .HasConversion(listConverter);
            modelBuilder.Entity<TaskInfo>()
                .Property(p => p.Arguments)
                .HasConversion(argConverter);
            modelBuilder.Entity<CodePreset>()
                .Property(p => p.Arguments)
                .HasConversion(argConverter);

            //添加索引
            modelBuilder.Entity<Log>()
                .HasIndex(p => p.Time);
            modelBuilder.Entity<Log>()
                .HasIndex(p => p.Type);
            modelBuilder.Entity<Log>()
                .HasIndex(p => p.TaskId);

            modelBuilder.Entity<TaskInfo>()
                .HasIndex(p => p.Type);
            modelBuilder.Entity<TaskInfo>()
                .HasIndex(p => p.CreateTime);
            modelBuilder.Entity<TaskInfo>()
                .HasIndex(p => p.FinishTime);
            modelBuilder.Entity<TaskInfo>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<CodePreset>()
                .HasIndex(p => p.Type);

            modelBuilder.Entity<Config>()
                .HasIndex(p => p.Key);
        }

        // public static void Migrate()
        // {
        //     if (File.Exists(dbName))
        //     {
        //         using SqliteConnection sqlite = new SqliteConnection(connectionString);
        //         sqlite.Open();
        //         SqliteCommand command = new SqliteCommand("select Value from Configs where Key == 'Version'", sqlite);
        //         SqliteDataReader reader = command.ExecuteReader();
        //         if (!reader.HasRows)
        //         {
        //             Migrate20230408(sqlite);
        //         }
        //         else
        //         {
        //             reader.Read();
        //             string version = reader.GetString(0);
        //         }
        //
        //         sqlite.Close();
        //     }
        //
        //     using var db = new FFmpegDbContext();
        //     var item = db.Configs.FirstOrDefault(p => p.Key == "Version");
        //     if (item == null)
        //     {
        //         db.Configs.Add(new Config("Version", CurrentVersion));
        //     }
        //     else
        //     {
        //         item.Value = CurrentVersion;
        //         db.Entry(item).State = EntityState.Modified;
        //     }
        //
        //     db.SaveChanges();
        //     db.Dispose();
        // }
        //
        // private static void Migrate20230408(SqliteConnection sqlite)
        // {
        //     Debug.WriteLine("数据库迁移：" + nameof(Migrate20230408));
        //     Console.WriteLine("数据库迁移：" + nameof(Migrate20230408));
        //     new SqliteCommand("CREATE INDEX IX_Logs_Type ON Logs (Type);", sqlite).ExecuteNonQuery();
        //     new SqliteCommand("CREATE INDEX IX_Logs_Time ON Logs (Time);", sqlite).ExecuteNonQuery();
        //     new SqliteCommand("CREATE INDEX IX_Logs_TaskId ON Logs (TaskId);", sqlite).ExecuteNonQuery();
        //
        //     new SqliteCommand("CREATE INDEX IX_Tasks_Type ON Tasks (Type);", sqlite).ExecuteNonQuery();
        //     new SqliteCommand("CREATE INDEX IX_Tasks_CreateTime ON Tasks (CreateTime);", sqlite).ExecuteNonQuery();
        //     new SqliteCommand("CREATE INDEX IX_Tasks_FinishTime ON Tasks (FinishTime);", sqlite).ExecuteNonQuery();
        //     new SqliteCommand("CREATE INDEX IX_Tasks_Status ON Tasks (Status);", sqlite).ExecuteNonQuery();
        //
        //     new SqliteCommand("CREATE INDEX IX_Presets_Type ON Presets (Type);", sqlite).ExecuteNonQuery();
        // }
    }
}