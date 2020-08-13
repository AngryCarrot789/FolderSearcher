using FolderSearcher.Files;
using FolderSearcher.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSearcher.Results
{
    public class ResultItemViewModel : BaseViewModel
    {
        // Using an icon because it's really simple
        // Can use a converter to convert icons to
        // BitmapImage/ImageSource which the Image
        // Control uses.

        private Icon _image;
        public Icon Image
        {
            get => _image;
            set => RaisePropertyChanged(ref _image, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => RaisePropertyChanged(ref _fileName, value);
        }

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set => RaisePropertyChanged(ref _filePath, value);
        }

        private long _fileSizeBytes;
        public long FileSizeBytes
        {
            get => _fileSizeBytes;
            set => RaisePropertyChanged(ref _fileSizeBytes, value);
        }

        private string _selection;
        public string Selection
        {
            get => _selection;
            set => RaisePropertyChanged(ref _selection, value);
        }

        // Doesn't need to be binded
        public FileType Type { get; set; }
    }
}
