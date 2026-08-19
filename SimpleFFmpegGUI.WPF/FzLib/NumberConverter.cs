using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.FzLib
{
    public static class NumberConverter
    {
        public static string SquareMeterToFitString(double lengthInMeter, int decimalDigits = 2, string mm = "平方毫米", string cm = "平方厘米", string m = "平方米", string ha = "公顷", string km = "平方千米")
        {
            if (lengthInMeter < 0)
            {
                throw new ArgumentException("长度为负数");
            }
            else if (lengthInMeter == double.NaN || lengthInMeter == double.PositiveInfinity)
            {
                throw new ArgumentException("长度无效");
            }
            else if (lengthInMeter == 0)
            {
                return 0 + m;
            }
            string format = "N" + decimalDigits.ToString();

            switch (lengthInMeter)
            {
                case double n when n < 1.0 / 1000000:
                    return (n * 1000000).ToString(format) + mm;

                case double n when n < 1.0 / 10000:
                    return (n * 10000).ToString(format) + cm;

                case double n when n > 10000:
                    return (n / 10000).ToString(format) + ha;

                case double n when n > 1000000:
                    return (n / 1000000).ToString(format) + km;

                default:
                    return lengthInMeter.ToString(format) + m;
            }
        }

        public static string MeterToFitString(double lengthInMeter, int decimalDigits = 2, string mm = "毫米", string cm = "厘米", string m = "米", string km = "千米")
        {
            if (lengthInMeter < 0)
            {
                throw new ArgumentException("长度为负数");
            }
            else if (lengthInMeter == double.NaN || lengthInMeter == double.PositiveInfinity)
            {
                throw new ArgumentException("长度无效");
            }
            else if (lengthInMeter == 0)
            {
                return 0 + m;
            }
            string format = "N" + decimalDigits.ToString();

            switch (lengthInMeter)
            {
                case double n when n < 1.0 / 1000:
                    return (n * 1000).ToString(format) + mm;

                case double n when n < 1.0 / 100:
                    return (n * 100).ToString(format) + cm;

                case double n when n > 1000:
                    return (n / 1000).ToString(format) + km;

                default:
                    return lengthInMeter.ToString(format) + m;
            }
        }

        public static string ByteToFitString(long size, int decimalDigits = 2, string B = "B", string KB = "KB", string MB = "MB", string GB = "GB", string TB = "TB")
        {
            if (size < 0)
            {
                return "";
            }
            double dSize = size;
            string format = "N" + decimalDigits.ToString();
            if (dSize < 1024)
            {
                return dSize.ToString() + B;
            }
            dSize /= 1024;
            if (dSize < 1024)
            {
                return dSize.ToString(format) + KB;
            }
            dSize /= 1024;
            if (dSize < 1024)
            {
                return dSize.ToString(format) + MB;
            }
            dSize /= 1024;
            if (dSize < 1024)
            {
                return dSize.ToString(format) + GB;
            }
            dSize /= 1024;
            return dSize.ToString(format) + TB;
        }

        public static string SecondToFitString(long seconds, bool week = false, string secondUnit = "秒", string minuteUnit = "分", string hourUnit = "小时", string dayUnit = "天", string weekUnit = "周")
        {
            string result = "";
            if (seconds < 0)
            {
                seconds = -seconds;
                result += "-";
            }
            const long secondPerWeek = 3600 * 24 * 7;
            const long secondPerDay = 3600 * 24;
            const long secondPerHour = 3600;
            const long secondPerMinute = 60;

            long per = 0;
            if (seconds >= secondPerWeek)
            {
                if (week)
                {
                    per = seconds / secondPerWeek;
                    result += per.ToString() + weekUnit;
                    seconds %= secondPerWeek;
                }
            }

            if (seconds >= secondPerDay)
            {
                per = seconds / secondPerDay;
                result += per.ToString() + dayUnit;
                seconds %= secondPerDay;
            }

            if (seconds >= secondPerHour)
            {
                per = seconds / secondPerHour;
                result += per.ToString() + hourUnit;
                seconds %= secondPerHour;
            }

            if (seconds >= secondPerMinute)
            {
                per = seconds / secondPerMinute;
                result += per.ToString() + minuteUnit;
                seconds %= secondPerMinute;
            }

            if (seconds > 0)
            {
                result += seconds + secondUnit;
            }

            return result;
        }
    }
}