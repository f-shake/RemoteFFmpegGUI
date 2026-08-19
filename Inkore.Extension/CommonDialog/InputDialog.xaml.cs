using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iNKORE.Extension.FzLib;
using iNKORE.UI.WPF.Modern.Controls;

namespace iNKORE.Extension.CommonDialog
{
    public partial class InputDialog : CommonDialog
    {
        private Func<string, bool> Verify { get; }
        private string AllowedCharacters { get; }

        internal InputDialog(Func<string, bool> verify = null, string allowedCharacters = null)
        {
            Verify = verify;
            AllowedCharacters = allowedCharacters;
            InitializeComponent();
        }

        private bool multiLines=false;
        public bool MultiLines
        {
            get => multiLines;
            set => this.SetValueAndNotify(ref multiLines, value, nameof(MultiLines));
        }
        private int maxLines = 1;
        public int MaxLines
        {
            get => maxLines;
            set => this.SetValueAndNotify(ref maxLines, value, nameof(MaxLines));
        }


        private string inputContent;

        public string InputContent
        {
            get => inputContent;
            set
            {
                if (Verify != null && Verify(value) == false)
                {
                    errorIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    errorIcon.Visibility = Visibility.Collapsed;
                }
                this.SetValueAndNotify(ref inputContent, value, nameof(InputContent));
            }
        }

        private void CommonDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (Verify != null && Verify(InputContent) == false)
            {
                args.Cancel = true;
            }
        }

        private void txt_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (AllowedCharacters != null && e.Text.Any(p => !AllowedCharacters.Contains(p)))
            {
                e.Handled = true;
            }
        }

        private async void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
            //先把后续的事情处理完，才能够设置焦点
            await Task.Yield();
            txt.Focus();
            Keyboard.Focus(txt);
        }
    }
}