using iNKORE.Extension.FzLib;
using System;
using System.ComponentModel;

namespace iNKORE.Extension.CommonDialog
{
    public abstract class DialogItem : INotifyPropertyChanged
    {
        public object Tag { get; set; }
        private string title;

        public string Title
        {
            get => title;
            set => this.SetValueAndNotify(ref title, value, nameof(Title));
        }

        private string detail;

        public string Detail
        {
            get => detail;
            set => this.SetValueAndNotify(ref detail, value, nameof(Detail));
        }

        public DialogItem(string title, string detail = null)
        {
            Title = title;
            Detail = detail;
        }

        public DialogItem()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class SelectDialogItem : DialogItem
    {
        public SelectDialogItem(string title, string detail, Action selectAction) : base(title, detail)
        {
            SelectAction = selectAction;
        }

        public SelectDialogItem(string title, string detail = null) : base(title, detail)
        {
        }

        public Action SelectAction
        {
            get => selectAction;
            set => this.SetValueAndNotify(ref selectAction, value, nameof(SelectAction));
        }

        private Action selectAction;
    }

    public class CheckDialogItem : DialogItem
    {
        public CheckDialogItem(string title, string detail = null) : base(title, detail)
        {
        }

        public CheckDialogItem(string title, string detail, bool isEnabled, bool isChecked) : base(title, detail)
        {
            IsEnabled = isEnabled;
            IsChecked = isChecked;
        }

        private bool isChecked;

        public bool IsChecked
        {
            get => isChecked;
            set => this.SetValueAndNotify(ref isChecked, value, nameof(IsChecked));
        }

        private bool isEnabled = true;

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.SetValueAndNotify(ref isEnabled, value, nameof(IsEnabled));
        }
    }
}