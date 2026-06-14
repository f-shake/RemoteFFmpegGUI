using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enterwell.Clients.Wpf.Notifications;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.Pages;
using SimpleFFmpegGUI.WPF.Panels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class PresetsPanelViewModel : ViewModelBase
    {
        private readonly PresetRepository presetRepository;
        private readonly PresetService presetService;

        public PresetsPanelViewModel(PresetRepository presetRepository, PresetService presetService)
        {
            this.presetRepository = presetRepository;
            this.presetService = presetService;
        }

        [ObservableProperty]
        private ObservableCollection<PresetEntity> presets;

        public INotificationMessageManager Manager { get; } = new NotificationMessageManager();

        public async Task UpdateTypeAsync(TaskType type)
        {
            Type = type;
            Presets = new ObservableCollection<PresetEntity>(await presetRepository.GetByTypeAsync(type));
        }

        [RelayCommand]
        private async Task DeleteAsync(PresetEntity preset)
        {
            Debug.Assert(CodeArgumentsViewModel != null);
            if (await CommonDialog.ShowYesNoDialogAsync("删除预设", $"是否删除预设：{preset.Name}？"))
            {
                try
                {
                    await presetRepository.SoftDeleteAsync(preset.Id);
                    Presets.Remove(preset);
                }
                catch (Exception ex)
                {
                    QueueErrorMessage("删除预设失败", ex);
                }
            }
        }

        [RelayCommand]
        private async Task UpdateAsync(PresetEntity preset)
        {
            Debug.Assert(CodeArgumentsViewModel != null);
            try
            {
                preset.Parameters = CodeArgumentsViewModel.GetArguments();
                await presetRepository.UpdateAsync(preset);
                QueueSuccessMessage($"预设“{preset.Name}”更新成功");
            }
            catch (Exception ex)
            {
                QueueErrorMessage($"更新预设失败", ex);
            }
        }

        [ObservableProperty]
        private TaskType type;

        [RelayCommand]
        private async Task MakeDefaultAsync(PresetEntity preset)
        {
            Debug.Assert(preset != null);
            try
            {
                await presetRepository.SetAsDefaultAsync(preset.Id);
                QueueSuccessMessage($"已将“{preset.Name}”设置为当前任务类型的默认预设");
            }
            catch (Exception ex)
            {
                QueueErrorMessage("设置默认预设失败", ex);
            }
        }

        [RelayCommand]
        private void Apply(PresetEntity preset)
        {
            Debug.Assert(CodeArgumentsViewModel != null);
            Debug.Assert(preset != null);
            CodeArgumentsViewModel.Update(Type, preset.Parameters);
            QueueSuccessMessage($"已加载预设“{preset.Name}”");
        }
        public CodeArgumentsPanelViewModel CodeArgumentsViewModel { get; set; }


        [RelayCommand]
        private async Task SaveToPresetAsync()
        {
            Debug.Assert(CodeArgumentsViewModel != null);
            var name = await CommonDialog.ShowInputDialogAsync("请输入新预设的名称");
            if (name == null)
            {
                return;
            }
            try
            {
                var preset = new PresetEntity { Name = name, Type = Type, Parameters = CodeArgumentsViewModel.GetArguments() };
                await presetRepository.AddAsync(preset);
                Presets.Add(preset);
            }
            catch (Exception ex)
            {
                QueueErrorMessage("新增预设失败", ex);
            }
        }
    }
}