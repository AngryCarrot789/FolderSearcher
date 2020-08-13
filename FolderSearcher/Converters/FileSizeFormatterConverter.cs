using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FolderSearcher.Converters
{
    public class FileSizeFormatterConverter : IValueConverter
    {
        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern int StrFormatByteSize(
            long fileSize,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
            int bufferSize);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                // Can use this to tell between
                // Files and Folders (folders have 
                // the max value of an int64, aka long)
                if (size == long.MaxValue)
                    return "";

                StringBuilder sizeString = new StringBuilder(20);
                StrFormatByteSize(size, sizeString, 20);
                return sizeString.ToString();
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (long)0;
        }
    }
}
