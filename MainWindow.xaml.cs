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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

using Vision;
using AimRobot.Properties;
using NetworkTables;

namespace AimRobot {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DateTime _lastframetime = DateTime.Now;
        int fps = 30;
        int _count;
        string _tmppath;

        DispatcherTimer dispatcherTimer;
        Camera _camera;
        ParticleFinder _particlefinder;
        NetworkTableConnection _ntconnection;
        NetworkTable _smartdashboard;
        Line _centerveritical;
        double Aspect;

        void WriteFile(byte[] bits, string path) {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                fs.Write(bits, 0, (int)bits.Length);
            }
        }

        public MainWindow() {
            InitializeComponent();

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _ntconnection = new NetworkTableConnection();
            _ntconnection.Connect();
            _smartdashboard = _ntconnection.GetTable("/SmartDashboard");

            _centerveritical = new Line();

            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            MouseDown +=new MouseButtonEventHandler(MainWindow_MouseDown);

            _tmppath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tmpimg.jpg");

            _direction.Background = Brushes.Firebrick;
            _direction.Foreground = Brushes.Yellow;
            _direction.FontSize = 20;

            _report.Background = Brushes.White;
            _report.TextAlignment = TextAlignment.Left;
            _report.FontFamily = new FontFamily("Courier New");
            _report.FontSize = 16;
            // Canvas.SetTop(_report, 0);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            dispatcherTimer.Start();

            Left = Settings.Default.Left;
            Top = Settings.Default.Top;
            Width = Settings.Default.Width;
            Height = Settings.Default.Height;

            Start();
        }

        public void Start() {
            _particlefinder = new ParticleFinder(Settings.Default.Luminance, false, 60 * 25);
            _camera = new Camera(Settings.Default.CameraURL, Settings.Default.FrameRate);
            _camera.Start();
        }

        // MainWindow_MouseDown opens the settings window, and restarts the camera with the 
        // new settings.
        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            AppSettings sp = new AppSettings(this);
            sp.Visibility = Visibility.Visible;

            if (_camera != null) {
                _camera.Stop();
                _camera = null;
                // _stop = true;
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (_camera != null)
                _camera.Stop();

            Settings.Default.Left = (int) Left;
            Settings.Default.Top = (int) Top;
            Settings.Default.Width = (int) Width;
            Settings.Default.Height = (int) Height;
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
            // double x = SizeToContent.Width;
            // throw new NotImplementedException();
        }

        void drawRobotLines() {
            RobotLocator rl = new RobotLocator(_particlefinder);
            int offset = rl.horizontaloffset + Settings.Default.HorizontalOffset;
            _centercamera.X1 = ((_particlefinder.imgwidth / 2) + offset);
            _centercamera.X2 = _centercamera.X1;

            string dir = string.Empty;
            if (offset > 0)
                dir = " Right";
            else if (offset < 0)
                dir = " Left";

            _direction.Text = offset.ToString() + dir;

            if (rl.targetmid != null) {
                _smartdashboard.SetDouble("offset", (double)offset);
                _smartdashboard.SetDouble("width", (double)rl.targetmid.width);
                _smartdashboard.SetDouble("height", (double)rl.targetmid.height);

                _targetmid.Width = rl.targetmid.width;
                _targetmid.Height = rl.targetmid.height;
                Canvas.SetTop(_targetmid, rl.targetmid.top);
                Canvas.SetLeft(_targetmid, rl.targetmid.left);

                Aspect = (rl.targetmid.height / rl.targetmid.width);
            }
            else {
                _targetmid.Width = 0;
                _targetmid.Height = 0;

                _smartdashboard.SetDouble("width", (double)0);
                _smartdashboard.SetDouble("height", (double)0);
                _smartdashboard.SetDouble("offset", (double)0);
            }
            if (rl.targetleft != null)
            {
                _targetleft.Width = rl.targetleft.width;
                _targetleft.Height = rl.targetleft.height;
                Canvas.SetTop(_targetleft, rl.targetleft.top);
                Canvas.SetLeft(_targetleft, rl.targetleft.left);

                Aspect = (rl.targetleft.height / rl.targetleft.width);
            }
            else
            {
                _targetleft.Width = 0;
                _targetleft.Height = 0; 
            }
            if (rl.targetright != null)
            {
                _targetright.Width = rl.targetright.width;
                _targetright.Height = rl.targetright.height;
                Canvas.SetTop(_targetright, rl.targetright.top);
                Canvas.SetLeft(_targetright, rl.targetright.left);

                Aspect = (rl.targetright.height / rl.targetright.width);
            }
            else
            {
                _targetright.Width = 0;
                _targetright.Height = 0;
            }
            //if (rl.right != null) {
            //    _rightgoal.Width = rl.right.width;
            //    _rightgoal.Height = rl.right.height;
            //    Canvas.SetTop(_rightgoal, rl.right.top);
            //    Canvas.SetLeft(_rightgoal, rl.right.left);
            //}

            //if (rl.top != null) {
            //    _topgoal.Width = rl.top.width;
            //    _topgoal.Height = rl.top.height;
            //    Canvas.SetTop(_topgoal, rl.top.top);
            //    Canvas.SetLeft(_topgoal, rl.top.left);
            //}
        }

        bool processCameraImage() {
            if ((_camera != null) && (_camera.NewImage)) {
                byte[] img = _camera.Image;

                if (img != null) {
                    BitmapImage bi = new BitmapImage();
                    using (MemoryStream m = new MemoryStream(img)) {
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = m;
                        bi.EndInit();
                    }

                    WriteFile(img, _tmppath);
                    string pngpath = _particlefinder.ProcessPath(_tmppath);
                    // BitmapImage bi = new BitmapImage(new Uri(pngpath));

                    _image.Source = bi;

                    drawRobotLines();
                }

                return true;
            }

            return false;
        }

        bool processVideoImage() {
            double dpi = 96;
            RenderTargetBitmap thumb = new RenderTargetBitmap((int)640, (int)480, dpi, dpi, PixelFormats.Pbgra32);

            thumb.Render(_video);

            Image img = new Image();
            img.Source = thumb;

            // PngBitmapEncoder encoder = new PngBitmapEncoder();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)img.Source));

            using (FileStream fs = new FileStream(_tmppath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                encoder.Save(fs);
            }

            _particlefinder.ProcessPath(_tmppath);
            drawRobotLines();

            return true;
        }

        void dispatcherTimer_Tick(object sender, EventArgs e) {
             bool newimg = processCameraImage();
            // bool newimg = processVideoImage();

            if ((_count % 30) == 0) {
                DateTime now = DateTime.Now;
                TimeSpan delta = now - _lastframetime;
                fps = (int)(30000.0 / delta.TotalMilliseconds);
                _lastframetime = now;

                if (_ntconnection.Connected) {
                    _connected.Background = Brushes.Green;
                    _connected.Text = "connected";
                }
                else {
                    _connected.Background = Brushes.Red;
                    _connected.Text = "disconnected";
                }
            }

            if (newimg) {
                _count++;

                _report.Text = string.Format("frame {0} fps {1} {2} Aspect {3}", _count, fps, _particlefinder.Particles.Count, Aspect);
            }
        }
    }
}
