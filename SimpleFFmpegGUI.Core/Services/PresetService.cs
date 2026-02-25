using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FzLib.Web;
using SimpleFFmpegGUI.Dto;

public class PresetService(PresetRepository repository)
{
    public async Task<bool> ExistsAsync(string name, TaskType type)
    {
        return await repository.ExistsAsync(name, type);
    }


    public async Task<ServiceResult<int>> AddPresetAsync(AddPresetRequest preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            return ServiceResult<int>.Failure("名称为空", HttpStatusCode.BadRequest);
        }

        if (await ExistsAsync(preset.Name, preset.Type))
        {
            return ServiceResult<int>.Failure($"已存在同类型同名称的预设: {preset.Name}", HttpStatusCode.Conflict);
        }

        var result = await repository.AddAsync(new CodePreset()
        {
            Name = preset.Name,
            Type = preset.Type,
            Arguments = preset.Arguments
        });
        return result.Id;
    }


    public async Task<ServiceResult> UpdatePresetAsync(int id, UpdatePresetRequest preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            return ServiceResult.Failure("名称为空", HttpStatusCode.BadRequest);
        }

        var existing = await repository.GetByIdAsync(id);
        if (existing == null)
        {
            return ServiceResult.Failure($"找不到ID为{id}的预设", HttpStatusCode.NotFound);
        }

        existing.Name = preset.Name;
        existing.Type = preset.Type;
        existing.Arguments = preset.Arguments;

        await repository.UpdateAsync(existing);
        return ServiceResult.Success();
    }


    public async Task<ServiceResult> DeletePresetAsync(int id)
    {
        if (id <= 0)
        {
            return ServiceResult<bool>.Failure("ID必须大于0", HttpStatusCode.BadRequest);
        }

        var affected = await repository.SoftDeleteAsync(id);

        if (affected == 0)
        {
            return ServiceResult<bool>.Failure($"找不到ID为{id}的预设", HttpStatusCode.NotFound);
        }

        return ServiceResult.Success();
    }

    public async Task<string> ExportAsync()
    {
        var presets = await repository.GetAllAsync();
        return presets.SerializeWithFriendlySettings();
    }

    public async Task<ServiceResult> ImportAsync(string json, bool overwrite = false)
    {
        var presets = json.DeserializeWithFriendlySettings<List<CodePreset>>();
        if (presets == null)
        {
            return ServiceResult.Failure("JSON反序列化失败", HttpStatusCode.BadRequest);
        }


        foreach (var preset in presets)
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                // throw new ArgumentException("预设名称不能为空");
                return ServiceResult.Failure("预设名称不能为空", HttpStatusCode.BadRequest);
            }

            preset.Id = 0;

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

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetDefaultPresetAsync(int id)
    {
        var preset = await repository.GetByIdAsync(id); // ?? throw new ArgumentException($"找不到ID为{id}的预设");
        if (preset == null)
        {
            return ServiceResult.Failure($"找不到ID为{id}的预设", HttpStatusCode.NotFound);
        }

        if (preset.IsDeleted)
        {
            // throw new InvalidOperationException($"预设已被删除");
            return ServiceResult.Failure($"预设已被删除", HttpStatusCode.BadRequest);
        }

        await repository.ClearAllDefaultsAsync(preset.Type);
        await repository.SetAsDefaultAsync(id);
        return ServiceResult.Success();
    }

    public async Task<int> DeletePresetsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Where(id => id > 0).Distinct().ToList();
        if (idList.Count == 0)
        {
            return 0;
        }

        return await repository.SoftDeleteRangeAsync(idList);
    }
}