
using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.Repositories;

public class PresetRepository(IDbContextFactory<FFmpegDbContext> dbFactory)
{
    public async Task<PresetEntity> AddAsync(PresetEntity preset)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Presets.Add(preset);
        await db.SaveChangesAsync();
        return preset;
    }

    public async Task<int> ClearAllDefaultsAsync(TaskType type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => p.Type == type && p.Default && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Default, false));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets.AnyAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<bool> ExistsAsync(string name, TaskType type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets.AnyAsync(p => !p.IsDeleted && p.Name == name && p.Type == type);
    }

    public async Task<List<PresetEntity>> GetAllAsync(bool includeDeleted = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => includeDeleted || !p.IsDeleted)
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<PresetEntity> GetByIdAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets.FindAsync(id);
    }

    public async Task<List<PresetEntity>> GetByTypeAsync(TaskType type, bool includeDeleted = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => p.Type == type && (includeDeleted || !p.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<PresetEntity> GetDefaultByTypeAsync(TaskType type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets.FirstOrDefaultAsync(p => p.Type == type && p.Default && !p.IsDeleted);
    }

    public async Task<int> SetAsDefaultAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Default, true));
    }

    public async Task<int> SoftDeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
    }

    public async Task<int> SoftDeleteAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
    }

    public async Task<int> SoftDeleteRangeAsync(IEnumerable<int> ids)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Presets
            .Where(p => ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
    }

    public async Task UpdateAsync(PresetEntity preset)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Presets.Update(preset);
        await db.SaveChangesAsync();
    }
}
