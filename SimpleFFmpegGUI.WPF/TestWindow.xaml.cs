using SimpleFFmpegGUI.WPF.FzLib.WPF.Converters;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.WPF.Converters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static SimpleFFmpegGUI.WPF.ViewModels.PerformanceTestLine;

namespace SimpleFFmpegGUI.WPF
{
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            ViewModel = this.SetDataContext<TestWindowViewModel>();
            InitializeComponent();
            CreateDataGrids();
            CreateTestItemsDataGrid();
        }

        public TestWindowViewModel ViewModel { get; set; }

        /// <summary>
        /// 动态创建表格
        /// </summary>
        private void CreateDataGrids()
        {
            grpSpeed.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.Fps), "0.00");
            grpSSIM.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.Ssim), "0.00%");
            grpPSNR.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.Psnr), "0.00");
            grpVMAF.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.Vmaf), "0.00");
            grpCpuUsage.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.CpuUsage), "0.00%");
            grpOutputSize.Content = GetDataGrid(nameof(PerformanceTestItemViewModel.OutputSize), "0.00");

            DataGrid GetDataGrid(string type, string format = null)
            {
                DataGrid dataGrid = new DataGrid()
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    CanUserReorderColumns = false,
                    CanUserResizeColumns = false,
                    CanUserResizeRows = false,
                    CanUserSortColumns = false,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    SelectionUnit = DataGridSelectionUnit.Cell,
                    IsReadOnly = true,
                    IsHitTestVisible = false,
                };
                dataGrid.SetBinding(DataGrid.IsEnabledProperty,
                    new Binding(nameof(TestWindowViewModel.IsTesting)) { Converter = new InverseBoolConverter() });
                dataGrid.SetBinding(DataGrid.ItemsSourceProperty,
                   new Binding(nameof(TestWindowViewModel.Tests)));
                DataGridTextColumn c = new DataGridTextColumn()
                {
                    Binding = new Binding(nameof(PerformanceTestLine.Header)),
                    IsReadOnly = true,
                };
                dataGrid.Columns.Add(c);
                for (int i = 0; i < CodecsCount; i++)
                {
                    c = new DataGridTextColumn()
                    {
                        Width = 108,
                        Header = ViewModel.Codecs[i].Name,
                        Binding = new Binding($"{nameof(PerformanceTestLine.Items)}[{i}].{type}")
                        {
                            Converter = new EmptyIfZeroConverter()
                        }
                    };
                    if (!string.IsNullOrEmpty(format))
                    {
                        c.Binding.StringFormat = format;
                    }
                    dataGrid.Columns.Add(c);
                }
                return dataGrid;
            }
        }

        private void CreateTestItemsDataGrid()
        {
            DataGrid dataGrid = new DataGrid()
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                CanUserResizeColumns = false,
                CanUserResizeRows = false,
                CanUserSortColumns = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionUnit = DataGridSelectionUnit.Cell,
            };
            dataGrid.SetBinding(DataGrid.IsEnabledProperty,
                new Binding(nameof(TestWindowViewModel.IsTesting)) { Converter = new InverseBoolConverter() });
            dataGrid.SetBinding(DataGrid.ItemsSourceProperty,
               new Binding(nameof(TestWindowViewModel.Tests)));
            var c = new DataGridTextColumn()
            {
                Binding = new Binding(nameof(PerformanceTestLine.Header)),
                IsReadOnly = true,
            };
            dataGrid.Columns.Add(c);
            for (int i = 0; i < CodecsCount; i++)
            {
                var codec = VideoCodec.VideoCodecs[i];
                var tc = new DataGridTemplateColumn
                {
                    Width = 108,
                    Header = codec.Name,
                    CellTemplate = new DataTemplate()
                };
                FrameworkElementFactory factory = new FrameworkElementFactory(typeof(CheckBox));
                factory.SetValue(MarginProperty, new Thickness(16, 0, 0, 0));
                factory.SetBinding(CheckBox.IsCheckedProperty, new Binding($"{nameof(PerformanceTestLine.Items)}[{i}].{nameof(PerformanceTestItemViewModel.IsChecked)}")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                tc.CellTemplate.VisualTree = factory;
                dataGrid.Columns.Add(tc);
            }
            c = new DataGridTextColumn()
            {
                Width = 108,
                Binding = new Binding(nameof(PerformanceTestLine.MBitrate)),
                Header = "码率 (Mbps)"
            };
            dataGrid.Columns.Add(c);
            grpTestItems.Content = dataGrid;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.SaveConfigs();

            if (ViewModel.IsTesting)
            {
                e.Cancel = true;
            }
        }
    }
}