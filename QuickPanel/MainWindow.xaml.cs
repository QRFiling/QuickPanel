using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;

namespace QuickPanel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        SoundPlayer openSoundPlayer = new SoundPlayer(Properties.Resources.OpenSound);
        SoundPlayer clickSoundPlayer = new SoundPlayer(Properties.Resources.ClickSound);

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
                Environment.Exit(0);

            DispatcherTimer timer = new DispatcherTimer();
            UserActivityHook hook = new UserActivityHook(true, false);
            timer.Interval = TimeSpan.FromSeconds(0.2);

            hook.OnMouseActivity += (s, a) =>
            {
                if (WindowState == WindowState.Minimized && !ScreenDetectionn.AreApplicationFullScreen())
                {
                    timer.Stop();
                    timer.Start();
                }
            };

            timer.Tick += (s, a) =>
            {
                timer.Stop();

                if (hook.isLeftButtonPressed && hook.isRightButtonPressed)
                    ShowWindow();
            };

            controlPanelLinksList.ItemsSource = QuickLinksService.GetControlPanelQuickLinks();

            foreach (FrameworkElement item in quickActionsPanel.Children)
            {
                if (item.Tag != null)
                {
                    item.MouseLeftButtonUp += (s, a) =>
                    {
                        if (QuickActions.RunActionFromName(item.Tag.ToString()))
                            HideWindow();
                    };
                }
            }

            HideWindow(false);
            LoadNotesToList();
        }

        async void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text) == false)
            {
                if (freezeSuggestions == false)
                {
                    suggestionsList.ItemsSource = await GetGoogleSearchSuggestions(searchBox.Text);
                    suggestionsListCompleteIndex = -1;
                }
                else freezeSuggestions = false;
            }
            else
            {
                await Task.Delay(200);
                suggestionsList.ItemsSource = null;
            }
        }

        int suggestionsListCompleteIndex = -1;
        bool freezeSuggestions = false;

        void searchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                if (suggestionsListCompleteIndex > 0) suggestionsListCompleteIndex--;
                else suggestionsListCompleteIndex = suggestionsList.Items.Count - 1;
                CompleteFromSuggestion();
            }
            else if (e.Key == Key.Down)
            {
                if (suggestionsListCompleteIndex < suggestionsList.Items.Count - 1) suggestionsListCompleteIndex++;
                else suggestionsListCompleteIndex = 0;
                CompleteFromSuggestion();
            }
            else if (e.Key == Key.Enter)
            {
                SearchInBrowser(searchBox.Text);
                e.Handled = true;
            }

            void CompleteFromSuggestion()
            {
                if (suggestionsList.HasItems)
                {
                    freezeSuggestions = true;
                    searchBox.Text = suggestionsList.Items[suggestionsListCompleteIndex].ToString();
                    searchBox.SelectionStart = searchBox.Text.Length;
                    e.Handled = true;
                }
            }
        }

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            SearchInBrowser(searchBox.Text);

        void suggestionsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            SearchInBrowser((sender as FrameworkElement).DataContext.ToString());

        async Task<IEnumerable<string>> GetGoogleSearchSuggestions(string text)
        {
            try
            {
                WebRequest request = WebRequest.Create("http://clients1.google.com/complete/search?hl=ru&output=toolbar&q=" + text);
                WebResponse response = await request.GetResponseAsync();

                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default))
                {
                    return XElement.Parse(reader.ReadToEnd()).Descendants().Where(w =>
                        w.FirstNode != null).Select(s => (s.FirstNode as XElement).FirstAttribute.Value);
                }
            }
            catch { }
            return null;
        }

        void SearchInBrowser(string text)
        {
            if (string.IsNullOrWhiteSpace(text) == false)
            {
                try
                {
                    if (Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute))
                    {
                        try { Process.Start(text); }
                        catch { Process.Start("https://yandex.ru/search/?text=" + text); }
                    }
                    else Process.Start("https://yandex.ru/search/?text=" + text);
                    HideWindow();
                }
                catch { }
            }
        }

        void controlPanelLinksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox.SelectedItem == null) return;

            (listBox.SelectedItem as QuickLinksService.Link).Start();
            listBox.SelectedItem = null;

            HideWindow();
        }

        void computerPreviewButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement button = computerPreviewButton.Child as FrameworkElement;
            double margin = button.FlowDirection == FlowDirection.LeftToRight ? listsGrid.ColumnDefinitions[0].ActualWidth -
                    (((computerLinksList.Items.Count - 1) * (85 + 4)) + 145) : 0;

            ThicknessAnimation animation = new ThicknessAnimation
            {
                To = new Thickness(margin, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new PowerEase { Power = 5 }
            };

            button.FlowDirection = margin == 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
            computerLinksList.BeginAnimation(MarginProperty, animation);
        }

        void Border_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e) => Close();
        void Border_MouseEnter(object sender, MouseEventArgs e) => HideWindow(false);

        void ShowWindow()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Maximized;

            dateLabel.BeginAnimation(OpacityProperty, null);
            dateLabel.Opacity = 0;
            searchBox.Focus();

            dateLabel.Text = $"Сегодня {DateTime.Now:dddd}";
            holidayLabel.Text = Holidays.GetHoliday(DateTime.Now);

            if (string.IsNullOrEmpty(holidayLabel.Text))
            {
                holidayLabel.Foreground = Brushes.Gray;
                holidayLabel.Text = "Праздников нет";
                holidayLabel.ToolTip = null;
            }
            else
            {
                holidayLabel.Foreground = Brushes.LightBlue;
                holidayLabel.ToolTip = holidayLabel.Text;
            }

            ThicknessAnimation animation = new ThicknessAnimation
            {
                From = new Thickness(0, 30, 0, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new PowerEase { Power = 5 }
            };

            DoubleAnimation animation2 = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                BeginTime = TimeSpan.FromSeconds(0.2)
            };

            DoubleAnimation animation3 = new DoubleAnimation
            {
                To = 0.8,
                From = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                BeginTime = TimeSpan.FromSeconds(0.4)
            };

            animation2.Completed += (s, a) =>
            {
                computerLinksList.ItemsSource = QuickLinksService.GetComputerQuickLinks();

                if (computerLinksList.Items.Count > 9)
                    computerPreviewButton.Visibility = Visibility.Visible;
            };

            contentGrid.BeginAnimation(MarginProperty, animation);
            listsGrid.BeginAnimation(OpacityProperty, animation2);
            dateLabel.BeginAnimation(OpacityProperty, animation3);
            openSoundPlayer.Play();
        }

        void HideWindow(bool withSound = true)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.05)
            };

            animation.Completed += (s, e) =>
            {
                searchBox.Text = string.Empty;
                WindowState = WindowState.Minimized;
                Hide();
                ShowInTaskbar = false;
            };

            listsGrid.BeginAnimation(OpacityProperty, animation);
            if (withSound) clickSoundPlayer.Play();
        }

        static string NotesFilePath = Path.Combine(Environment.GetFolderPath
            (Environment.SpecialFolder.MyDocuments), "QuickPanel", "UserNotes.xml");

        void Border_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            XDocument document = LoadNotesFromFile();
            XElement xElement = new XElement("Note");

            xElement.SetAttributeValue("Title", string.IsNullOrWhiteSpace(noteTitle.Text) ?
                    "Без заголовка" : noteTitle.Text);

            xElement.SetAttributeValue("Date", DateTime.Now);
            XElement root = document.Element("Notes");
            xElement.Add(noteText.Text);
            root.Add(xElement);

            Directory.CreateDirectory(Path.GetDirectoryName(NotesFilePath));
            document.Save(NotesFilePath);
            LoadNotesToList(root);

            noteTitle.Text = noteText.Text = string.Empty;
        }

        XDocument LoadNotesFromFile()
        {
            XDocument document = new XDocument(new XDeclaration("1.0", "windows-1251", null));
            document.Add(new XElement("Notes"));

            if (File.Exists(NotesFilePath))
                document = XDocument.Load(NotesFilePath);

            return document;
        }

        void LoadNotesToList(XElement root = null)
        {
            if (root == null) root = LoadNotesFromFile().Element("Notes");
            notesList.ItemsSource = root.Elements("Note").Reverse();
            notesBlockTitle.Text = $"Заметки {notesList.Items.Count} шт";
        }

        void Border_MouseLeftButtonUp_3(object sender, MouseButtonEventArgs e)
        {
            XElement element = (sender as FrameworkElement).DataContext as XElement;
            Clipboard.SetText(element.FirstNode.ToString());
            HideWindow();
        }

        void Border_MouseLeftButtonUp_4(object sender, MouseButtonEventArgs e)
        {
            XElement element = (sender as FrameworkElement).DataContext as XElement;
            XDocument document = element.Document;

            element.Remove();
            LoadNotesToList(document.Element("Notes"));

            Directory.CreateDirectory(Path.GetDirectoryName(NotesFilePath));
            document.Save(NotesFilePath);
        }
    }
}
