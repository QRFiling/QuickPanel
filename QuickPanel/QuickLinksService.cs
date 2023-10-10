using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace QuickPanel
{
    class QuickLinksService
    {
        public class Link
        {
            public BitmapImage Icon { get; set; }
            public string Title { get; set; }
            public string Command { get; set; }
            public bool IsHeaderLink { get; set; }
            public string HeaderSubtitle { get; set; }
            public double IconSize { get; set; }

            public Link(string title, BitmapImage icon, string command, string headerSubtitle = null, double iconSize = 35)
            {
                Title = title;
                Icon = icon;
                Command = command;
                IconSize = iconSize;

                if (headerSubtitle != null)
                {
                    IsHeaderLink = true;
                    HeaderSubtitle = headerSubtitle;
                }
            }

            public void Start()
            {
                string[] parts = Command.Split(';');
                if (parts.Length == 2) Process.Start(parts[0], parts[1]);
            }
        }

        static BitmapImage GetIcon(string name) =>
            new BitmapImage(new Uri($"pack://application:,,,/Resources/{name}.png"));

        public static List<Link> GetControlPanelQuickLinks()
        {
            return new List<Link>
            {
                new Link("Панель управления", GetIcon("ControlPanelIcon"), "control;", "В новом окне ⇱"),
                new Link("Система и безопасность", GetIcon("ShieldIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\5"),
                new Link("Сеть и Интернет", GetIcon("WifiIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\3"),
                new Link("Оборудование и звук", GetIcon("HardwareIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\2"),
                new Link("Программы", GetIcon("ApplicationIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\8"),
                new Link("Учетные записи", GetIcon("UsersIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\9"),
                new Link("Персонализация", GetIcon("PersonalizationIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\1"),
                new Link("Часы, язык и регион", GetIcon("TimeIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\6"),
                new Link("Специальные возможности", GetIcon("AccessibilityIcon"), "explorer;shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}\\7"),
            };
        }

        public static List<Link> GetComputerQuickLinks()
        {
            List<Link> links = new List<Link>
            {
                new Link("Мой компьютер", GetIcon("ComputerIcon"), "explorer;::{20d04fe0-3aea-1069-a2d8-08002b30309d}", "В новом окне ⇱"),
                new Link("Очистить корзину", GetIcon("RecycleBinIcon"), "explorer;shell:RecycleBinFolder")
            };

            try
            {
                links.AddRange(PhysicalDriveVolumesHelper.GetCurrentOSVolumes().Select(s =>
                    new Link($"{(string.IsNullOrEmpty(s.VolumeLabel) ? "Диск" : s.VolumeLabel)} {s.Name} ({BytesToString(s.AvailableFreeSpace)} своб)",
                    IconHelper.GetFolderIcon(s.Name), "explorer;" + s.Name)));
            }
            catch { }

            try
            {
                DirectoryInfo startMenuFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu"));

                if (startMenuFolder.Exists)
                {
                    links.AddRange(startMenuFolder.GetFiles().OrderByDescending(o => o.CreationTime).Select(s => new Link(s.Name.Replace(s.Extension, string.Empty),
                        IconHelper.GetFileIcon(s.FullName), "explorer;" + s.FullName, iconSize: 25)));
                }
            }
            catch { }

            return links;
        }

        static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
