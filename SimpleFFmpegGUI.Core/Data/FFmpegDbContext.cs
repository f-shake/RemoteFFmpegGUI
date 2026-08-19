using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Converters;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Data
{
    public class FFmpegDbContext : DbContext
    {
        public FFmpegDbContext(DbContextOptions<FFmpegDbContext> options) : base(options)
        {
        }

        public DbSet<LogEntity> Logs { get; set; }

        public DbSet<PresetEntity> Presets { get; set; }

        public DbSet<TaskEntity> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //对于非结构化数据，采用Json的方式进行存储
            var listConverter = new EFJsonConverter<List<InputParameters>>();
            var argConverter = new EFJsonConverter<OutputParameters>();
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TaskEntity>()
                .Property(p => p.Inputs)
                .HasConversion(listConverter);
            modelBuilder.Entity<TaskEntity>()
                .Property(p => p.Parameters)
                .HasConversion(argConverter);
            modelBuilder.Entity<PresetEntity>()
                .Property(p => p.Parameters)
                .HasConversion(argConverter);

            //添加索引
            modelBuilder.Entity<LogEntity>()
                .HasIndex(p => p.Time);
            modelBuilder.Entity<LogEntity>()
                .HasIndex(p => p.Type);
            modelBuilder.Entity<LogEntity>()
                .HasIndex(p => p.TaskId);

            modelBuilder.Entity<TaskEntity>()
                .HasIndex(p => p.Type);
            modelBuilder.Entity<TaskEntity>()
                .HasIndex(p => p.CreateTime);
            modelBuilder.Entity<TaskEntity>()
                .HasIndex(p => p.FinishTime);
            modelBuilder.Entity<TaskEntity>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<PresetEntity>()
                .HasIndex(p => p.Type);
        }
    }
}
