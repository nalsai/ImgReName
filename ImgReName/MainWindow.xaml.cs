using Squirrel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ImgReName
{
    public partial class MainWindow : Window
    {

        static async void Updater()
        {
            using (var mgr = new UpdateManager(@"https://nalsai.de/imgrename/download"))
            {
                try
                {
                    await mgr.UpdateApp();
                }
                catch
                {
                    // (╯°□°）╯︵ ┻━┻
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Updater();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void DropThings(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ReName(files);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Bilder/Videos|*.jpg; *.png; *.tiff; *.tif; *.bmp; *.mp4; *.mov; *.avi|" +
                         "Alle Dateien|*.*"
            };

            // Display OpenFileDialog by calling ShowDialog method
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                ReName(dlg.FileNames);
            }
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<object> args = e.Argument as List<object>;

            int Errors = 0;
            object NamingMethod = args[0];
            int i = 0;
            foreach (string path in (string[])args[1])
            {
                i++;
                DateTime realDate = new DateTime(2000, 1, 1, 1, 1, 1);
                DateTime fileCreatedDate;
                DateTime fileChangedDate;
                string date;
                string newpath;
                string directory;
                string newdirectory;
                string name;
                string newname;
                string extension;
                int Year;
                int Month;
                int Day;
                int Hour;
                int Minute;
                int Second;

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        // Get Directory, Name and Extension of File
                        directory = Path.GetDirectoryName(path);
                        name = Path.GetFileNameWithoutExtension(path);
                        extension = Path.GetExtension(path);

                        // Get Date
                        if (extension == ".mp4" || extension == ".mov" || extension == ".avi")
                        {
                            fileCreatedDate = File.GetCreationTime(path);
                            fileChangedDate = File.GetLastWriteTime(path);
                            if (fileChangedDate < fileCreatedDate)
                                realDate = fileChangedDate;
                            else
                                realDate = fileCreatedDate;
                        }
                        else
                        {
                            BitmapSource img = BitmapFrame.Create(fs);
                            BitmapMetadata md = (BitmapMetadata)img.Metadata;
                            date = md.DateTaken;
                            if (string.IsNullOrEmpty(date))
                            {
                                (sender as BackgroundWorker).ReportProgress(i, "エ" + "Kein Aufnahmedatum für " + Path.GetFileName(path) + " gefunden." + Environment.NewLine);
                                Errors++;
                                continue;
                            }
                            realDate = DateTime.Parse(date);
                        }
                    }

                ChooseName:
                    Year = realDate.Year;
                    Month = realDate.Month;
                    Day = realDate.Day;
                    Hour = realDate.Hour;
                    Minute = realDate.Minute;
                    Second = realDate.Second;

                    newname = "IMG";
                    if (NamingMethod == ImgRenameMichael)
                    {
                        newname = Number2String(Year - 1999, true) + Number2String(Month, true);
                        if (Day <= 9)
                            newname += Day;
                        else
                            newname += Number2String(Day - 9, true);

                        newname += (Hour * 3600 + Minute * 60 + Second);

                        if (extension == ".mp4" || extension == ".mov")
                            newname = "VID_" + newname;
                    }
                    else if (NamingMethod == img_date_time)
                    {

                        if (extension == ".mp4" || extension == ".mov")
                            newname = "VID_" + realDate.ToString("yyyyMMdd") + "_" + realDate.ToString("HHmmss");
                        else
                            newname = "IMG_" + realDate.ToString("yyyyMMdd") + "_" + realDate.ToString("HHmmss");
                    }
                    newpath = Path.Combine(directory, realDate.ToString("yyyy-MM-dd"), newname + extension);
                    newdirectory = Path.Combine(directory, realDate.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(newdirectory);
                    if (File.Exists(newpath))
                    {
                        realDate = realDate.AddSeconds(1);
                        goto ChooseName;
                    }
                    File.Move(path, newpath);
                    (sender as BackgroundWorker).ReportProgress(i, Path.GetFileName(path) + " wurde nach " + Path.Combine(realDate.ToString("yyyy-MM-dd"), newname + extension) + " verschoben." + Environment.NewLine);
                }
                catch (Exception exc)
                {
                    (sender as BackgroundWorker).ReportProgress(i, "エ" + "Fehler: " + exc + Environment.NewLine);
                    Errors++;
                    continue;
                }
            }

            (sender as BackgroundWorker).ReportProgress(i, Environment.NewLine);
            if (Errors == 1)
            {
                (sender as BackgroundWorker).ReportProgress(i, "エ" + "Abgeschlossen mit einem Fehler." + Environment.NewLine);
            }
            else if (Errors > 1)
            {
                (sender as BackgroundWorker).ReportProgress(i, "エ" + "Abgeschlossen mit " + Errors + " Fehlern." + Environment.NewLine);
            }
            else
                (sender as BackgroundWorker).ReportProgress(i, "Abgeschlossen ohne Fehler." + Environment.NewLine);

            e.Result = true;
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgrBar.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                string msg = (string)e.UserState;
                if (msg.Contains("エ"))
                {
                    Run run = new Run(" " + msg.TrimStart('エ')) { Foreground = Brushes.Red };
                    debugLog.Inlines.Add(run);
                }
                else
                    debugLog.Inlines.Add(" " + msg);
            }
            ScrollViewer.ScrollToBottom();
        }

        void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if ((bool)e.Result)
                    BrowseBtn.IsEnabled = true;
                else
                    debugLog.Inlines.Add("(╯°□°）╯︵ ┻━┻");
            }
            catch
            { 
                debugLog.Inlines.Add("(╯°□°）╯︵ ┻━┻"); 
            }
        }

        public void ReName(string[] PathList)
        {

            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.WorkerReportsProgress = true;
                worker.DoWork += Worker_DoWork;
                worker.ProgressChanged += Worker_ProgressChanged;
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

                debugLog.Inlines.Clear();
                ProgrBar.Minimum = 0;
                ProgrBar.Maximum = PathList.Length;
                BrowseBtn.IsEnabled = false;

                List<object> arguments = new List<object>
                {
                    MethodSelect.SelectedItem,
                    PathList
                };

                worker.RunWorkerAsync(arguments);
            }
        }

        private string Number2String(int number, bool isCaps)
        {
            char c = (char)((isCaps ? 65 : 97) + (number - 1));
            return c.ToString();
        }
    }
}