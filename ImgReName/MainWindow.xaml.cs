using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

/**
 * Sucht nach dem Aufnahmezeitpunktm der ausgewählten Bilder, 
 * verschiebt sie in einen Ordner, der nach dem Datum benannt ist 
 * und benennt sie entsprechend ihrem Aufnhamezeitpunkt.
 * @author Nils Fürniß
 * @version 1.0
 */

namespace ImgReName
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            int Errors = 0;

            object NamingMethod = MethodSelect.SelectedItem;

            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Bilder|*.jpg; *.png; *.tiff; *.tif; *.bmp|" +
                            "Alle Dateien|*.*"
            };

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                foreach (String path in dlg.FileNames)
                {
                    DateTime realDate;
                    string date;
                    string newpath;
                    string directory;
                    string newdirectory;
                    string name;
                    string newname;
                    string extension;

                    try
                    {
                        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                        {

                            // Get Date
                            debugLog.Inlines.Add(Environment.NewLine + "Suche Aufnahmedatum von \"" + path + "\"");
                            BitmapSource img = BitmapFrame.Create(fs);
                            BitmapMetadata md = (BitmapMetadata)img.Metadata;
                            date = md.DateTaken;
                            if (String.IsNullOrEmpty(date))
                            {
                                Run run = new Run(Environment.NewLine + "Kein Aufnahmedatum gefunden." + Environment.NewLine)
                                {
                                    Foreground = Brushes.Red
                                };
                                debugLog.Inlines.Add(run);
                                ScrollViewer.ScrollToBottom();
                                DoEvents();
                                Errors++;
                                continue;
                            }
                            debugLog.Inlines.Add(Environment.NewLine + "Aufnahmedatum: " + date);
                            realDate = DateTime.Parse(date);

                            // Get Directory, Name and Extension of File
                            directory = System.IO.Path.GetDirectoryName(path);
                            name = System.IO.Path.GetFileNameWithoutExtension(path);
                            extension = System.IO.Path.GetExtension(path);

                            int Year = (int)realDate.Year;
                            int Month = (int)realDate.Month;
                            int Day = (int)realDate.Day;
                            int Hour = (int)realDate.Hour;
                            int Minute = (int)realDate.Minute;
                            int Second = (int)realDate.Second;


                            newname = "IMG";
                            if (NamingMethod == ImgRenameMichael)
                            {
                                newname = Number2String(Year - 1999, true) + Number2String(Month, true);
                                if (Day <= 9)
                                    newname = newname + Day;
                                else
                                    newname = newname + Number2String(Day - 9, true);

                                newname = newname + (Hour * 3600 + Minute * 60 + Second);
                            }
                            else if (NamingMethod == img_date_time)
                            {
                                newname = "IMG_" + realDate.ToString("yyyyMMdd") + "_" + realDate.ToString("HHmmss");
                            }
                            debugLog.Inlines.Add(Environment.NewLine + "Neuer Name: " + newname);
                            newpath = System.IO.Path.Combine(directory, realDate.ToString("yyyy-MM-dd"), newname + extension);
                            newdirectory = System.IO.Path.Combine(directory, realDate.ToString("yyyy-MM-dd"));
                            debugLog.Inlines.Add(Environment.NewLine + "Erstelle Ordner " + realDate.ToString("yyyy-MM-dd"));
                            Directory.CreateDirectory(newdirectory);
                        }
                        debugLog.Inlines.Add(Environment.NewLine + "Verschiebe nach \"" + newpath + "\"" + Environment.NewLine);
                        ScrollViewer.ScrollToBottom();
                        DoEvents();
                        File.Move(path, newpath);
                    }
                    catch (Exception exc)
                    {
                        //if (exc == System.IO.IOException)
                        //{
                        ///
                        //}
                        //else
                        //{
                            Run run = new Run(Environment.NewLine + "Fehler: " + exc + Environment.NewLine)
                            {
                                Foreground = Brushes.Red
                            };
                            debugLog.Inlines.Add(run);
                            ScrollViewer.ScrollToBottom();
                            DoEvents();
                            Errors++;
                            continue;
                        //}
                    }
                }

                if (Errors > 0)
                {
                    Run run = new Run(Environment.NewLine + "Abgeschlossen mit " + Errors + " Fehlern." + Environment.NewLine)
                    {
                        Foreground = Brushes.Red
                    };
                    debugLog.Inlines.Add(run);
                }
                else
                    debugLog.Inlines.Add(Environment.NewLine + "Abgeschlossen ohne Fehler." + Environment.NewLine);

            }
        }

        public void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame(true);
            Dispatcher.CurrentDispatcher.BeginInvoke
            (
            DispatcherPriority.Background,
            (SendOrPostCallback)delegate (object arg)
            {
                var f = arg as DispatcherFrame;
                f.Continue = false;
            },
            frame
            );
            Dispatcher.PushFrame(frame);
        }

        private String Number2String(int number, bool isCaps)
        {
            Char c = (Char)((isCaps ? 65 : 97) + (number - 1));
            return c.ToString();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine(MethodSelect.SelectedItem);
        }
    }
}