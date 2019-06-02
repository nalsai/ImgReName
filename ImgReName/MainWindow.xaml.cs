using Squirrel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                try
                {
                    // Get Directory, Name and Extension of File
                    string dir = Path.GetDirectoryName(path);
                    string name = Path.GetFileNameWithoutExtension(path);
                    string ext = Path.GetExtension(path);

                    // Convert extension to lowercase
                    if (ext.Any(char.IsUpper))
                        ext = ext.ToLower();

                    // Get Date
                    DateTime dateTaken;
                    if (ext == ".mp4" || ext == ".mov" || ext == ".avi")
                    {
                        DateTime fileCreatedDate = File.GetCreationTime(path);
                        DateTime fileChangedDate = File.GetLastWriteTime(path);
                        if (fileChangedDate < fileCreatedDate)
                            dateTaken = fileChangedDate;
                        else
                            dateTaken = fileCreatedDate;
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                        {
                            BitmapSource img = BitmapFrame.Create(fs);
                            BitmapMetadata md = (BitmapMetadata)img.Metadata;
                            string date = md.DateTaken;
                            if (string.IsNullOrEmpty(date))
                            {
                                (sender as BackgroundWorker).ReportProgress(i, "エ" + "Kein Aufnahmedatum für " + Path.GetFileName(path) + " gefunden." + Environment.NewLine);
                                Errors++;
                                continue;
                            }
                            dateTaken = DateTime.Parse(date);
                        }
                    }

                ChooseName:

                    string newname = "";
                    if (NamingMethod == ImgRenameMichael)
                    {
                        newname = Number2String(dateTaken.Year - 1999, true) + Number2String(dateTaken.Month, true);
                        int Day = dateTaken.Day;
                        if (Day <= 9)
                            newname += Day;
                        else
                            newname += Number2String(Day - 9, true);

                        newname += (dateTaken.Hour * 3600 + dateTaken.Minute * 60 + dateTaken.Second);

                        if (ext == ".mp4" || ext == ".mov" || ext == ".avi")
                            newname = "VID_" + newname;
                    }
                    else if (NamingMethod == img_date_time)
                    {
                        if (ext == ".mp4" || ext == ".mov" || ext == ".avi")
                            newname = "VID_" + dateTaken.ToString("yyyyMMdd") + "_" + dateTaken.ToString("HHmmss");
                        else
                            newname = "IMG_" + dateTaken.ToString("yyyyMMdd") + "_" + dateTaken.ToString("HHmmss");
                    }
                    string newpath = Path.Combine(dir, dateTaken.ToString("yyyy-MM-dd"), newname + ext);
                    Directory.CreateDirectory(Path.Combine(dir, dateTaken.ToString("yyyy-MM-dd")));
                    if (File.Exists(newpath))
                    {
                        dateTaken = dateTaken.AddSeconds(1);
                        goto ChooseName;
                    }
                    File.Move(path, newpath);
                    (sender as BackgroundWorker).ReportProgress(i, Path.GetFileName(path) + " wurde nach " + Path.Combine(dateTaken.ToString("yyyy-MM-dd"), newname + ext) + " verschoben." + Environment.NewLine);
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