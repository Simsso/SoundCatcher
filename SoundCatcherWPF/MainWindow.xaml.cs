using System;
using System.Windows;
using System.Windows.Controls;

namespace SoundCatcherWPF
{
    public partial class MainWindow : Window
    {
        Control control;

        public MainWindow()
        {
            InitializeComponent();
            control = new Control(this);
        }

        // main button clicked event
        private void Button_RecordStartStop_Click(object sender, RoutedEventArgs e)
        {
            switch (control.ToggleApplicationState())
            {
                // new application state:

                case ApplicationState.Ready:
                    CheckBox_AutoCut.IsEnabled = true;
                    Ellipse_Record.Visibility = Visibility.Visible;
                    Rectangle_Stop.Visibility = Visibility.Hidden;
                    TextBlock_ButtonText.Text = "Record";
                    break;
                case ApplicationState.Recording:
                    CheckBox_AutoCut.IsEnabled = false;
                    RefreshByteLabel(0);
                    Ellipse_Record.Visibility = Visibility.Hidden;
                    Rectangle_Stop.Visibility = Visibility.Visible;
                    TextBlock_ButtonText.Text = "Stop";
                    break;
            }
        }

        public void RefreshByteLabel(ulong bytesRecorded)
        {
            Label_Bytes.Dispatcher.InvokeAsync((Action)(() =>
                {
                    // show kBytes
                    Label_Bytes.Content = Math.Round((bytesRecorded / 1000.0), 0).ToString();
                }));
        }

        private void Window_Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            control.WindowClosing(sender, e);
        }

        private void CheckBox_AutoCut_Checked(object sender, RoutedEventArgs e)
        {
            ((CheckBox)sender).IsChecked = control.SetCutEnable(true);
        }

        private void CheckBox_AutoCut_Unchecked(object sender, RoutedEventArgs e)
        {
            ((CheckBox)sender).IsChecked = control.SetCutEnable(false);
        }
    }
}
