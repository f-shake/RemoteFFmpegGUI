using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WPF;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        /// <summary>
        /// 绑定的所属元素（由 SetDataContext 注入），用于把通知显示到正确的窗口
        /// </summary>
        internal FrameworkElement Owner { get; set; }

        protected TMessage SendMessage<TMessage>(TMessage message) where TMessage : class
        {
            return WeakReferenceMessenger.Default.Send(message);
        }
        protected void QueueSuccessMessage(string message)
        {
            ShowNotification('S', message, null);
        }
        protected void QueueErrorMessage(string message, Exception ex = null)
        {
            ShowNotification('E', message, ex);
        }
        private void ShowNotification(char type, string message, Exception ex)
        {
            if (Owner is not null)
            {
                try
                {
                    var builder = Owner.CreateMessage();
                    if (type == 'S')
                    {
                        builder.QueueSuccess(message);
                    }
                    else if (ex is null)
                    {
                        builder.QueueError(message);
                    }
                    else
                    {
                        builder.QueueError(message, ex);
                    }
                    return;
                }
                catch
                {
                    // 元素已脱离窗口树（如视图被关闭/导航走）时 CreateMessage 可能抛异常，回退到全局广播
                }
            }
            SendMessage(new QueueMessagesMessage(type, message, ex));
        }
    }

    public static class ViewModelExtension
    {
        public static TVM SetDataContext<TVM>(this FrameworkElement element) where TVM : ViewModelBase
        {
            TVM viewModel = App.ServiceProvider.GetRequiredService<TVM>();
            element.DataContext = viewModel;
            viewModel.Owner = element;
            return viewModel;
        }

        /// <summary>
        /// 解决CommunityToolkit.MVVM的ObservableObject中属性赋值时只有在新旧值不同时才会进行通知的问题
        /// </summary>
        /// <typeparam name="TVM">ViewModel类型</typeparam>
        /// <typeparam name="TP">属性类型</typeparam>
        /// <param name="viewModel">视图模型</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="newValue">新的值</param>
        /// <exception cref="ArgumentException"></exception>
        public static void SetAndNotify<TVM, TP>(this TVM viewModel, string propertyName, TP newValue) where TVM : ObservableObject
        {
            var type = typeof(TVM);
            var property = type.GetRuntimeProperty(propertyName) ?? throw new ArgumentException("找不到对应的Property");
            var oldValue = (TP)property.GetValue(viewModel);


            IEnumerable<MethodInfo> methods = null;

            if (EqualityComparer<TP>.Default.Equals(oldValue, newValue))
            {
                var field = type.GetRuntimeFields()
                    .Where(p => p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase) || p.Name.Equals("@" + propertyName, StringComparison.InvariantCultureIgnoreCase))
                    .Where(p => p.CustomAttributes.Any(p => p.AttributeType == typeof(ObservablePropertyAttribute)))
                    .FirstOrDefault() ?? throw new ArgumentException("找不到对应的Field");

                methods = type.GetRuntimeMethods().Where(p => p.Name.StartsWith("On"));
                InvokeMethodIfExisted($"On{propertyName}Changing", oldValue);
                InvokeMethodIfExisted($"On{propertyName}Changing", oldValue, newValue);
                InvokeMethodIfExisted($"OnPropertyChanging", propertyName);
                field.SetValue(viewModel, newValue);
                InvokeMethodIfExisted($"On{propertyName}Changed", oldValue);
                InvokeMethodIfExisted($"On{propertyName}Changed", oldValue, newValue);
                InvokeMethodIfExisted($"OnPropertyChanged", propertyName);
                var notifyPropertyChangedForAttributes = field.GetCustomAttributes<NotifyPropertyChangedForAttribute>();
                if (notifyPropertyChangedForAttributes.Any())
                {
                    foreach (var attr in notifyPropertyChangedForAttributes)
                    {
                        foreach (var p in attr.PropertyNames)
                        {
                            InvokeMethodIfExisted($"OnPropertyChanged", new PropertyChangedEventArgs(p));
                        }
                    }
                }
            }
            else
            {
                property.SetValue(viewModel, newValue);
            }


            void InvokeMethodIfExisted(string methodName, params object[] args)
            {
                var method = methods.FirstOrDefault(p => p.Name == methodName
                && p.GetParameters().Length == args.Length
                && Enumerable.SequenceEqual(p.GetParameters().Select(p => p.ParameterType), args.Select(p => p.GetType())));
                method?.Invoke(viewModel, args);
            }
        }
    }
}