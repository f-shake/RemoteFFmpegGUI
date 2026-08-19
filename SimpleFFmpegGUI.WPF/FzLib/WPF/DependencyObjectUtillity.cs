using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF
{
    public static class DependencyObjectExtension
    {
        /// <summary>
        /// 寻找对象中第一个属于该类型的子元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T GetChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            DependencyObject child = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                child = VisualTreeHelper.GetChild(obj, i) as Visual;
                if (child != null && child is T)
                {
                    break;
                }
                else if (child != null)
                {
                    child = child.GetChild<T>();
                    if (child != null && child is T)
                    {
                        break;
                    }
                }
            }
            return child as T;
        }

        /// <summary>
        /// 寻找对象中所有属于该类型的子元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<T> GetChilds<T>(this DependencyObject obj) where T : DependencyObject
        {
            List<T> results = new List<T>();
            Get(obj);
            void Get(DependencyObject visual)
            {
                Visual child;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
                {
                    child = VisualTreeHelper.GetChild(visual, i) as Visual;
                    if (child != null && child is T t)
                    {
                        results.Add(t);
                    }
                    if (child != null)
                    {
                        Get(child);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// 寻找该对象的第一个属于该类型的父元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T GetParent<T>([NotNull] this FrameworkElement obj) where T : DependencyObject
        {
            DependencyObject result = obj.Parent;
            while (result != null && !(result is T))
            {
                if (result is FrameworkElement f)
                {
                    result = f.Parent;
                }
                else
                {
                    return null;
                }
            }
            return result as T;
        }
    }
}