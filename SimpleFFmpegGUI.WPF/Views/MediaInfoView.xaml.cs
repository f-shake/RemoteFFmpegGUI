using SimpleFFmpegGUI.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleFFmpegGUI.WPF.Views
{
    public partial class MediaInfoView : UserControl
    {
        public MediaInfoView()
        {
            ViewModel = this.SetDataContext<MediaInfoPageViewModel>();
            InitializeComponent();
        }

        public MediaInfoPageViewModel ViewModel { get; set; }

        public void SetFile(string file)
        {
            ViewModel.FilePath = file;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            var files = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? e.Data.GetData(DataFormats.FileDrop) as string[]
                : null;
            if (files?.Length == 1 && System.IO.File.Exists(files[0]))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            var files = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? e.Data.GetData(DataFormats.FileDrop) as string[]
                : null;
            if (files?.Length == 1 && System.IO.File.Exists(files[0]))
            {
                ViewModel.FilePath = files[0];
            }
        }

        private void TextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            Keyboard.ClearFocus();
        }
    }
}