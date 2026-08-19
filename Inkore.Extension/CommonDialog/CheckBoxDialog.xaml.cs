using iNKORE.Extension.FzLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace iNKORE.Extension.CommonDialog
{
    public partial class CheckBoxDialog : CommonDialog
    {
        internal CheckBoxDialog()
        {
            InitializeComponent();
        }

        private IList<CheckDialogItem> items;

        public IList<CheckDialogItem> Items
        {
            get => items;
            set => this.SetValueAndNotify(ref items, value, nameof(Items));
        }

        private bool needAtLeastOneCheck;

        public bool NeedAtLeastOneCheck
        {
            get => needAtLeastOneCheck;
            set
            {
                this.SetValueAndNotify(ref needAtLeastOneCheck, value, nameof(NeedAtLeastOneCheck));
                if (value)
                {
                    IsPrimaryButtonEnabled = Items != null && Items.Any(p => p.IsChecked);
                }
                else
                {
                    IsPrimaryButtonEnabled = true;
                }
            }
        }

        public int SelectedIndex { get; private set; } = -1;

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsPrimaryButtonEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (NeedAtLeastOneCheck)
            {
                IsPrimaryButtonEnabled = Items != null && Items.Any(p => p.IsChecked);
            }
        }
    }
}