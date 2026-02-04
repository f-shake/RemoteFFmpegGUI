
using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Repositories;

public class PresetRepository(FFmpegDbContext db)
{
    public async Task<CodePreset> AddAsync(CodePreset preset)
    {
        db.Presets.Add(preset);
        await db.SaveChangesAsync();
        return preset;
    }

    // 批量操作
    public async Task<int> ClearAllDefaultsAsync(TaskType type)
    {
        return await db.Presets
            .Where(p => p.Type == type && p.Default && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Default, false));
    }

    // 查询方法
    public Task<bool> ExistsAsync(int id) => db.Presets.AnyAsync(p => p.Id == id && !p.IsDeleted);

    public Task<bool> ExistsAsync(string name, TaskType type) => db.Presets.AnyAsync(p => !p.IsDeleted && p.Name == name && p.Type == type);

    public Task<List<CodePreset>> GetAllAsync(bool includeDeleted = false) =>
            db.Presets
                .Where(p => includeDeleted || !p.IsDeleted)
                .OrderBy(p => p.Type)
                .ThenBy(p => p.Name)
                .ToListAsync();

    public async Task<CodePreset> GetByIdAsync(int id) => await db.Presets.FindAsync(id);

    public Task<List<CodePreset>> GetByTypeAsync(TaskType type, bool includeDeleted = false) =>
        db.Presets
            .Where(p => p.Type == type && (includeDeleted || !p.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync();

    public Task<CodePreset> GetDefaultByTypeAsync(TaskType type) =>
        db.Presets.FirstOrDefaultAsync(p => p.Type == type && p.Default && !p.IsDeleted);

    public async Task<int> SetAsDefaultAsync(int id) => await db.Presets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Default, true));

    public async Task<int> SoftDeleteAsync(int id) =>
        await db.Presets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));

    public async Task<int> SoftDeleteRangeAsync(IEnumerable<int> ids) =>
        await db.Presets
            .Where(p => ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));

    public async Task UpdateAsync(CodePreset preset)
    {
        db.Presets.Update(preset);
        await db.SaveChangesAsync();
    }
}
