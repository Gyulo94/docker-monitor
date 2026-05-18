using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace docker_monitor.Dialogs
{
    public partial class LogExportDialog : FluentWindow
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public bool IsConfirmed { get; private set; }

        public LogExportDialog()
        {
            InitializeComponent();
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            
            StartDatePicker.SelectedDate = StartDate;
            EndDatePicker.SelectedDate = EndDate;
            
            UpdateSummary();
        }

        private void OnTodayClick(object sender, RoutedEventArgs e)
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            UpdatePickers();
        }

        private void OnLast7DaysClick(object sender, RoutedEventArgs e)
        {
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            UpdatePickers();
        }

        private void OnLast30DaysClick(object sender, RoutedEventArgs e)
        {
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            UpdatePickers();
        }

        private void UpdatePickers()
        {
            StartDatePicker.SelectedDate = StartDate;
            EndDatePicker.SelectedDate = EndDate;
            UpdateSummary();
        }

        private void OnDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StartDatePicker == null || EndDatePicker == null) return;
            if (StartDatePicker.SelectedDate.HasValue) StartDate = StartDatePicker.SelectedDate.Value.Date;
            if (EndDatePicker.SelectedDate.HasValue) EndDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (SelectionSummary != null)
            {
                SelectionSummary.Text = $"{StartDate:yyyy-MM-dd} 부터 {EndDate:yyyy-MM-dd} 까지";
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (StartDate > EndDate)
            {
                System.Windows.MessageBox.Show("시작 날짜가 종료 날짜보다 늦을 수 없습니다.", "알림", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
    }
}
