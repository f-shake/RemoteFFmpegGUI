
using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class PresetService(PresetRepository repository)
{
    public async Task<int> AddOrUpdatePresetAsync(string name, TaskType type, OutputArguments arguments)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("名称为空");
        }

        var existing = (await repository.GetByTypeAsync(type))
            .FirstOrDefault(p => p.Name == name);

        if (existing != null)
        {
            existing.Arguments = arguments;
            await repository.UpdateAsync(existing);
            return existing.Id;
        }
        else
        {
            // 新增
            var preset = new CodePreset
            {
                Name = name,
                Type = type,
                Arguments = arguments
            };
            var result = await repository.AddAsync(preset);
            return result.Id;
        }
    }

    public async Task<CodePreset> AddPresetAsync(string name, TaskType type, OutputArguments arguments)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("名称为空");
        }

        if (await repository.ExistsAsync(name, type))
        {
            throw new InvalidOperationException($"名为{name}的预设已存在");
        }

        var preset = new CodePreset
        {
            Name = name,
            Type = type,
            Arguments = arguments
        };

        return await repository.AddAsync(preset);
    }

    public async Task<bool> DeletePresetAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "ID必须大于0");
        }

        var affected = await repository.SoftDeleteAsync(id);

        if (affected == 0)
        {
            throw new KeyNotFoundException($"找不到ID为{id}的预设");
        }

        return affected > 0;
    }

    public async Task<string> ExportAsync()
    {
        var presets = await repository.GetAllAsync();
        return presets.SerializeWithFriendlySettings();
    }

    public async Task ImportAsync(string json, bool overwrite = false)
    {
        var presets = json.DeserializeWithFriendlySettings<List<CodePreset>>() ?? throw new ArgumentException("无效的JSON数据");


        foreach (var preset in presets)
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                throw new ArgumentException("预设名称不能为空");
            }

            preset.Id = 0; // 确保是新记录

            if (overwrite && await repository.ExistsAsync(preset.Name, preset.Type))
            {
                var existing = (await repository.GetByTypeAsync(preset.Type))
                    .FirstOrDefault(p => p.Name == preset.Name);

                if (existing != null)
                {
                    await repository.SoftDeleteAsync(existing.Id);
                }
            }

            await repository.AddAsync(preset);
        }
    }

    public async Task SetDefaultPresetAsync(int id)
    {
        var preset = await repository.GetByIdAsync(id) ?? throw new ArgumentException($"找不到ID为{id}的预设");

        if (preset.IsDeleted)
        {
            throw new InvalidOperationException($"预设已被删除");
        }

        // 先清除同类型的默认预设
        await repository.ClearAllDefaultsAsync(preset.Type);

        // 设置新的默认预设
        await repository.SetAsDefaultAsync(id);
    }
    public async Task<int> TryDeletePresetsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Where(id => id > 0).Distinct().ToList();
        if (idList.Count == 0)
        {
            return 0;
        }

        return await repository.SoftDeleteRangeAsync(idList);
    }
}