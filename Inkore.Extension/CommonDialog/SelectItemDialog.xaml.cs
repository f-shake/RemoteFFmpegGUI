using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace iNKORE.Extension.CommonDialog
{
    public partial class SelectItemDialog : CommonDialog
    {
        internal SelectItemDialog()
        {
            InitializeComponent();
        }

        private IList<SelectDialogItem> items;

        public IList<SelectDialogItem> Items
        {
            get => items;
            set => this.SetValueAndNotify(ref items, value, nameof(Items));
        }

        public int SelectedIndex { get; private set; } = -1;

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndex = (sender as ListView).SelectedIndex;
            var item = (sender as ListView).SelectedItem as SelectDialogItem;

            Hide();
            item.SelectAction?.Invoke();
        }
    }
}