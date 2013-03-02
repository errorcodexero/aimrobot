using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using AimRobot.Properties;

namespace AimRobot {
    /// <summary>
    /// Interaction logic for AppSettings.xaml
    /// </summary>
    public partial class AppSettings : Window {
        MainWindow _mw;

        public AppSettings(MainWindow mw) {
            InitializeComponent();

            _mw = mw;

            _cameraurl.Text = Settings.Default.CameraURL;
            _luminance.Text = Settings.Default.Luminance.ToString();
            _framerate.Text = Settings.Default.FrameRate.ToString();
            _trim.Text = Settings.Default.Trim.ToString();

            Closing += new System.ComponentModel.CancelEventHandler(AppSettings_Closing);
        }

        void AppSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            _mw.Start();
        }


        private void _save_Click(object sender, RoutedEventArgs e) {
            try {
                Settings.Default.Luminance = Byte.Parse(_luminance.Text);
                Settings.Default.CameraURL = _cameraurl.Text;
                Settings.Default.FrameRate = UInt32.Parse(_framerate.Text);
                Settings.Default.Trim = Int32.Parse(_trim.Text);

                Settings.Default.Save();

                Close();

                _mw.Start();
            }
            catch (Exception ex) {
                MessageBoxResult result = MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
