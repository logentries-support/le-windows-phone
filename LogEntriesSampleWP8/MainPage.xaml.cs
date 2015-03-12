using System;
using System.Linq;
using Microsoft.Phone.Controls;
using LogEntries;
using System.Collections.Generic;

namespace LogEntriesSampleWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void DebugButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Dictionary<string, DateTime> dict = new Dictionary<string, DateTime>();

            dict.Add("today", DateTime.Now);
            dict.Add("tomorrow", DateTime.Now.AddDays(1));

            LogEntriesService.Debug(dict.ToDictionary(entry => (object)entry.Key, entry => (object)entry.Value));
        }

        private void LogButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Log(DateTime.Now.ToLongTimeString());
        }

        private void ErrorButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Error(DateTime.Now.ToLongTimeString());
        }

        private void InfoButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Info(DateTime.Now.ToLongTimeString());
        }

        private void CriticalButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Critical(DateTime.Now.ToLongTimeString());
        }

        private void EmergencyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Emergency(DateTime.Now.ToLongTimeString());
        }

        private void WarningButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Warning(DateTime.Now.ToLongTimeString());
        }

        private void NoticeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Notice(DateTime.Now.ToLongTimeString());
        }

        private void AlertButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LogEntriesService.Alert(DateTime.Now.ToLongTimeString());
        }

        private void CrashButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            throw new Exception("unhandled exception");
        }
    }
}