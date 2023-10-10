using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Windows;

namespace QuickPanel
{
    class QuickActions
    {
        public static bool RunActionFromName(string name)
        {
            if (name.ToLower() == "txtfile")
                return ActionTXTFile();
            if (name.ToLower() == "audioinputdialog")
                return ActionAudioInputDialog();
            if (name.ToLower() == "translate")
                return ActionTranslate();

            return false;
        }

        static bool ActionTXTFile()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = Path.Combine(desktop, $"{DateTime.Now:d MMMM HH;mm;ss}.txt");

                File.Create(fileName).Close();
                Process.Start(fileName);

                return true;
            }
            catch { return false; }
        }

        static bool ActionAudioInputDialog()
        {
            try
            {
                Process.Start("control", "mmsys.cpl");
                return true;
            }
            catch { return false; }
        }

        static bool ActionTranslate()
        {
            try
            {
                string param = Clipboard.ContainsText() ? "/?text=" + HttpUtility.UrlEncode(Clipboard.GetText()) : string.Empty;
                Process.Start("https://translate.yandex.ru" + param);
                return true;
            }
            catch { return false; }
        }
    }
}
