using FolderSearcher.Files;
using FolderSearcher.Results;
using FolderSearcher.Utilities;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FolderSearcher.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _searchFor;
        public string SearchFor
        {
            get => _searchFor;
            set => RaisePropertyChanged(ref _searchFor, value);
        }

        private string _startFolder;
        public string StartFolder
        {
            get => _startFolder;
            set => RaisePropertyChanged(ref _startFolder, value);
        }

        private SearchPreference _searchPreferences;
        public SearchPreference SearchPreferences
        {
            get => _searchPreferences;
            set => RaisePropertyChanged(ref _searchPreferences, value);
        }

        private bool _caseSensitive;
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => RaisePropertyChanged(ref _caseSensitive, value);
        }

        private bool _searchRecursive;
        public bool SearchRecursive
        {
            get => _searchRecursive;
            set => RaisePropertyChanged(ref _searchRecursive, value);
        }

        private bool _ignoreExtension;
        public bool IgnoreExtension
        {
            get => _ignoreExtension;
            set => RaisePropertyChanged(ref _ignoreExtension, value);
        }

        private int _foldersSearched;
        public int FoldersSearched
        {
            get => _foldersSearched;
            set => RaisePropertyChanged(ref _foldersSearched, value);
        }

        private int _filesSearched;
        public int FilesSearched
        {
            get => _filesSearched;
            set => RaisePropertyChanged(ref _filesSearched, value);
        }

        private string _currentlySearching;
        public string CurrentlySearching
        {
            get => _currentlySearching;
            set => RaisePropertyChanged(ref _currentlySearching, value);
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set => RaisePropertyChanged(ref _isSearching, value);
        }

        // No binding needed
        public bool CanSearch { get; set; }

        // Will hold a list of viewmodels. In the view you can create controls based
        // On the viewmodels by setting the control's datacontext as "{Binding}"
        public ObservableCollection<ResultItemViewModel> Results { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand CancelSearchCommand { get; }
        public ICommand SelectStartFolderCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand ClearResultsCommand { get; }

        public MainViewModel()
        {
            Results = new ObservableCollection<ResultItemViewModel>();

            SearchCommand = new Command(Find);
            CancelSearchCommand = new Command(CancelSearch);
            SelectStartFolderCommand = new Command(SelectStartFolderPath);
            ExportResultsCommand = new Command(ExportResultsToFolder);
            ClearResultsCommand = new Command(Clear);

            SearchRecursive = true;
            SearchPreferences = SearchPreference.Files;
            StartFolder = @"F:\testfolderdx"; //Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            SearchFor = "";
            IgnoreExtension = true;
        }

        public void ExportResultsToFolder()
        {
            VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
            fbd.UseDescriptionForTitle = true;
            fbd.Description = "Select a directory for the found items to be copied to";
            if (fbd.ShowDialog() == true)
                if (fbd.SelectedPath.IsDirectory())
                    foreach (ResultItemViewModel result in Results)
                        if (result.FilePath.IsFile())
                            File.Copy(result.FilePath, Path.Combine(fbd.SelectedPath, result.FileName));
        }

        public void CancelSearch()
        {
            SetSearchingStatus(false);
        }

        public void SelectStartFolderPath()
        {
            VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
            fbd.UseDescriptionForTitle = true;
            fbd.Description = "Select a directory to start the search in";
            if (fbd.ShowDialog() == true)
            {
                if (fbd.SelectedPath.IsDirectory())
                    StartFolder = fbd.SelectedPath;
                else
                    MessageBox.Show("Selected folder doesn't exist");
            }
        }

        public void Clear()
        {
            Results.Clear();
        }

        public void ClearSearchCounters()
        {
            FilesSearched = 0;
            FoldersSearched = 0;
        }

        public void SetSearchingStatus(bool isSearching)
        {
            IsSearching = isSearching;
            CanSearch = isSearching;
            CurrentlySearching = "";
        }

        public void Find()
        {
            try
            {
                // This method will be async, so cancel any search currenly active
                // Although according to the view logic that shouldn't be possible
                // To do multiple searches because the search button becomes disabled.
                CancelSearch();
                ClearSearchCounters();

                if (!string.IsNullOrEmpty(SearchFor))
                {
                    if (StartFolder.IsDirectory())
                    {
                        Clear();
                        SetSearchingStatus(true);

                        Task.Run(() =>
                        {
                            string searchText = CaseSensitive ? SearchFor : SearchFor.ToLower();

                            if (SearchRecursive)
                            {
                                StartSearchRecursively(searchText);
                            }

                            else
                            {
                                StartSearchNonRecursively(searchText);
                            }

                            SetSearchingStatus(false);
                        });
                    }
                }
            }
            catch (Exception e) { MessageBox.Show($"{e.Message} -- Cancelling Search"); CancelSearch(); }
        }

        public void StartSearchRecursively(string searchText)
        {
            string startFolder = StartFolder;
            switch (SearchPreferences)
            {
                case SearchPreference.Files:
                    {
                        // In order to search recursively you need to basically run
                        // A function from within itself (so void s(), inside that
                        // it will run s() at some point.

                        // I will make a local method to simplify this

                        void DirectorySearch(string toSearchDir)
                        {
                            // This will loop through every folder and then do the same thing
                            // For subfolders and so on indefinitely until there's no
                            // more sub folders

                            foreach(string folder in Directory.GetDirectories(toSearchDir))
                            {
                                // Cancel search if needed
                                if (!CanSearch) return;

                                foreach(string file in Directory.GetFiles(folder))
                                {
                                    if (!CanSearch) return;

                                    SearchFileName(file, searchText);
                                }

                                FoldersSearched++;

                                // This is what makes this run recursively, the fact you
                                // can the same function in the same function... sort of
                                DirectorySearch(folder);
                            }
                        }

                        DirectorySearch(startFolder);

                        // Also need to search through every file in the start folders too...
                        foreach (string file in Directory.GetFiles(startFolder))
                        {
                            if (!CanSearch) return;

                            SearchFileName(file, searchText);
                        }
                    }
                    break;

                case SearchPreference.Folders:
                    {
                        void DirectorySearch(string toSearchDir)
                        {
                            foreach (string folder in Directory.GetDirectories(toSearchDir))
                            {
                                // Cancel search if needed
                                if (!CanSearch) return;

                                SearchFolderName(folder, searchText);

                                DirectorySearch(folder);
                            }
                        }

                        DirectorySearch(startFolder);
                    }
                    break;

                case SearchPreference.FileContents:
                    {
                        void DirectorySearch(string toSearchDir)
                        {
                            foreach (string folder in Directory.GetDirectories(toSearchDir))
                            {
                                // Cancel search if needed
                                if (!CanSearch) return;

                                foreach (string file in Directory.GetFiles(folder))
                                {
                                    if (!CanSearch) return;

                                    ReadAndSearchFile(file, searchText, true);
                                }

                                FoldersSearched++;

                                DirectorySearch(folder);
                            }
                        }

                        DirectorySearch(startFolder);

                        foreach (string file in Directory.GetFiles(startFolder))
                        {
                            if (!CanSearch) return;

                            ReadAndSearchFile(file, searchText, true);
                        }
                    }
                    break;

                case SearchPreference.All:
                    {
                        void DirectorySearch(string toSearchDir)
                        {
                            foreach (string folder in Directory.GetDirectories(toSearchDir))
                            {
                                // Cancel search if needed
                                if (!CanSearch) return;

                                SearchFolderName(folder, searchText);

                                foreach (string file in Directory.GetFiles(folder))
                                {
                                    if (!CanSearch) return;

                                    bool hasFoundFile = SearchFileName(file, searchText);

                                    // this stops there from being duplicated items.
                                    // if it's already found the item above, dont search
                                    // the contents because that's just pointless.
                                    if (!hasFoundFile)
                                    {
                                        ReadAndSearchFile(file, searchText, false);
                                    }
                                }

                                FoldersSearched++;

                                DirectorySearch(folder);
                            }
                        }

                        DirectorySearch(startFolder);

                        // Also need to search through every file in the start folders too...

                        foreach (string file in Directory.GetFiles(startFolder))
                        {
                            if (!CanSearch) return;

                            bool hasFoundFile = SearchFileName(file, searchText);

                            // this stops there from being duplicated items.
                            // if it's already found the item above, dont search
                            // the contents because that's just pointless.
                            if (!hasFoundFile)
                            {
                                ReadAndSearchFile(file, searchText, false);
                            }
                        }
                    }
                    break;
            }
        }

        public void StartSearchNonRecursively(string searchText)
        {
            // Non recursive is a big less messy
            // Because this is async, you could end up changing StartFolder and it would
            // Mess up the search a bit.
            string startFolder = StartFolder;
            switch (SearchPreferences)
            {
                case SearchPreference.Files:
                    foreach (string file in Directory.GetFiles(startFolder))
                    {
                        if (!CanSearch) return;
                        SearchFileName(file, searchText);
                    }
                    break;

                case SearchPreference.Folders:
                    foreach (string file in Directory.GetDirectories(startFolder))
                    {
                        if (!CanSearch) return;
                        SearchFolderName(file, searchText);
                    }
                    break;

                case SearchPreference.FileContents:
                    foreach (string file in Directory.GetFiles(startFolder))
                    {
                        if (!CanSearch) return;
                        ReadAndSearchFile(file, searchText, true);
                    }
                    break;

                case SearchPreference.All:
                    foreach (string file in Directory.GetDirectories(startFolder))
                    {
                        if (!CanSearch) return;
                        SearchFolderName(file, searchText);
                    }

                    foreach (string file in Directory.GetFiles(startFolder))
                    {
                        if (!CanSearch) return;
                        bool hasFoundFile = SearchFileName(file, searchText);

                        // Again this stops items from being searched when they've
                        // Already been found in the above code from the name.
                        if (!hasFoundFile)
                        {
                            ReadAndSearchFile(file, searchText, false);
                        }
                    }
                    break;
            }
        }

        public string GetFileName(string original)
        {
            if (IgnoreExtension)
                return Path.GetFileNameWithoutExtension(original);
            else
                return Path.GetFileName(original);
        }

        public bool SearchFileName(string name, string searchText)
        {
            CurrentlySearching = name;
            string fPath = CaseSensitive ? name : name.ToLower();
            if (GetFileName(fPath).Contains(searchText))
            {
                ResultFound(fPath, searchText);
                FilesSearched++;
                return true;
            }
            FilesSearched++;
            return false;
        }

        public bool SearchFolderName(string name, string searchText)
        {
            string dPath = CaseSensitive ? name : name.ToLower();
            if (dPath.GetDirectoryName().Contains(searchText))
            {
                ResultFound(dPath, searchText);
                FoldersSearched++;
                return true;
            }

            FoldersSearched++;
            return false;
        }

        public void ReadAndSearchFile(string file, string searchText, bool increaseSearchedFiles)
        {
            try
            {
                CurrentlySearching = file;

                // FileStreams are better because they wont load
                // The entire file into memory which is very good
                // If the file to be searched is maybe 1 gigabyte.
                using (FileStream fs = File.OpenRead(file))
                {
                    // Read the file in chunks of 1kb.
                    byte[] b = new byte[1024];
                    while(fs.Read(b, 0, b.Length) > 0)
                    {
                        // Cancels the search if CanSearch is false
                        if (!CanSearch) return;

                        // Get text from buffer
                        string txt = Encoding.ASCII.GetString(b);

                        // Inline convert the text to lower if CaseSensitive is false
                        if ((CaseSensitive ? txt : txt.ToLower()).Contains(searchText))
                        {
                            ResultFound(file, searchText);
                            break;
                        }
                    }
                }

                if (increaseSearchedFiles)
                    FilesSearched++;
            }
            catch(Exception e) { MessageBox.Show($"{e.Message} -- Cancelling Search"); CancelSearch(); }
        }

        public void ResultFound(string path, string selection)
        {
            ResultItemViewModel result = CreateResultFromPath(path, selection);
            if (result != null)
                AddResultAsync(result);
        }

        public ResultItemViewModel CreateResultFromPath(string path, string selectionText)
        {
            if (path.IsFile())
            {
                try
                {
                    FileInfo fInfo = new FileInfo(path);
                    ResultItemViewModel result = new ResultItemViewModel()
                    {
                        Image = IconHelper.GetIconOfFile(path, false, false),
                        FileName = fInfo.Name,
                        FilePath = fInfo.FullName,
                        Selection = selectionText,
                        FileSizeBytes = fInfo.Length,
                        Type = FileType.File
                    };

                    return result;
                }
                catch (Exception e) { MessageBox.Show($"{e.Message}"); return null; }
            }

            else if (path.IsDirectory())
            {
                try
                {
                    DirectoryInfo dInfo = new DirectoryInfo(path);
                    ResultItemViewModel result = new ResultItemViewModel()
                    {
                        Image = IconHelper.GetIconOfFile(path, false, true),
                        FileName = dInfo.Name,
                        FilePath = dInfo.FullName,
                        Selection = selectionText,
                        // This is the flag used before
                        // In the FileSizeFormatterConverter
                        FileSizeBytes = long.MaxValue,
                        Type = FileType.Folder
                    };

                    return result;
                }
                catch (Exception e) { MessageBox.Show($"{e.Message}"); return null; }
            }

            return null;
        }

        public void AddResultAsync(ResultItemViewModel result)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Results.Add(result);
            });
        }

        public void RemoveResult(ResultItemViewModel result)
        {
            Results.Remove(result);
        }
    }
}
