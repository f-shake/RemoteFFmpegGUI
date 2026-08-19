using iNKORE.Extension.FzLib;
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

namespace iNKORE.Extension.CommonDialog
{
    public partial class DetailTextDialog : CommonDialog
    {
        internal DetailTextDialog()
        {
            InitializeComponent();
        }

        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        private string detail;

        public string Detail
        {
            get => detail;
            set => this.SetValueAndNotify(ref detail, value, nameof(Detail));
        }

        private string icon;

        public string Icon
        {
            get => icon;
            set => this.SetValueAndNotify(ref icon, value, nameof(Icon));
        }

        private Brush iconBrush;

        public Brush IconBrush
        {
            get => iconBrush;
            set => this.SetValueAndNotify(ref iconBrush, value, nameof(IconBrush));
        }
    }
}