using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleFFmpegGUI.WPF.FzLib
{
    public static class INotifyPropertyChangedExtension
    {
        public static void Notify(this INotifyPropertyChanged obj, params string[] names)
        {
            foreach (var name in names)
            {
                Raise(obj, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(name));
            }
        }

        public static void Notify(this INotifyPropertyChanged obj, [CallerMemberName] string name = null)
        {
            Raise(obj, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(name));
        }

        private static void Raise<TEventArgs>(object source, string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            MulticastDelegate eventDelegate = null;
            FieldInfo field = null;
            Type type = source.GetType();
            while (true)
            {
                field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    break;
                }
                if (type.BaseType != null)
                {
                    type = type.BaseType;
                }
                else
                {
                    throw new Exception($"Can't find event \"{eventName}\"");
                }
            }
            eventDelegate = (MulticastDelegate)field.GetValue(source);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { source, eventArgs });
                }
            }
        }

        public static void SetValueAndNotify<T>(this INotifyPropertyChanged obj, ref T field, T value, params string[] names)
        {
            field = value;
            obj.Notify(names);
        }

        public static void SetValueAndNotify<T>(this INotifyPropertyChanged obj, ref T field, T value, [CallerMemberName] string name = null)
        {
            field = value;
            obj.Notify(name);
        }
    }
}