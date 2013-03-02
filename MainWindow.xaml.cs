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

    public partial class MainWindow : Window
    {
        DateTime _lastframetime = DateTime.Now;
        int fps = 10;
        int _framecount;
        string _tmppath;
        AppSettings _settings; 

        DispatcherTimer dispatcherTimer;
        CameraMJPG _camera;
        // Camera _camera;
        ParticleFinder _particlefinder;
        NetworkTableConnection _ntconnection;
        NetworkTable _smartdashboard;
        double _robottrim; // as entered into the robot
        int _trim;         // as recorded in Settings
        bool _aiming = false;
        int _targetoffset;     // offset from center to selected target
        int _lasttargetoffset; // used to see if we're still moving after we've "stopped"
        StreamWriter _log;
        DateTime _logstart;

        void WriteFile(byte[] bits, string path) {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                fs.Write(bits, 0, (int)bits.Length);
            }
        }

        string timestamp() {
            TimeSpan dt = DateTime.Now - _logstart;

            return dt.ToString("mm:ss.ff");
        }

        public MainWindow() {
            InitializeComponent();

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _ntconnection = new NetworkTableConnection();
            _ntconnection.Connect();
            _smartdashboard = _ntconnection.GetTable("/SmartDashboard");

            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            _tmppath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tmpimg.jpg");

            _direction.Background = Brushes.Firebrick;
            _direction.Foreground = Brushes.Yellow;
            _direction.FontSize = 20;

            _report.Background = Brushes.White;
            _report.TextAlignment = TextAlignment.Left;
            _report.FontFamily = new FontFamily("Courier New");
            _report.FontSize = 16;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();

            Left = Settings.Default.Left;
            Top = Settings.Default.Top;
            Width = Settings.Default.Width;
            Height = Settings.Default.Height;

            Start();
        }

        public void Start() {
            _settings = null; 
            _particlefinder = new ParticleFinder(Settings.Default.Luminance, false, 60 * 25);
            _camera = new CameraMJPG(Settings.Default.CameraURL, Settings.Default.FrameRate);
            // _camera = new Camera(Settings.Default.CameraURL, Settings.Default.FrameRate);

            _trim = Settings.Default.Trim;

            _camera.Start();
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            try {
                if (_camera != null)
                    _camera.Stop();

                Settings.Default.Left = (int) Left;
                Settings.Default.Top = (int) Top;
                Settings.Default.Width = (int) Width;
                Settings.Default.Height = (int) Height;

                Settings.Default.Save();
            }
            catch {
            }
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
            // double x = SizeToContent.Width;
            // throw new NotImplementedException();
        }

        void reportAiming() 
        {
            bool a = _smartdashboard.GetBool("aiming");

            if (_log != null)
                _log.WriteLine("{0}\t{1}\t{2}", timestamp(), _framecount, _targetoffset);

            if (a && !_aiming) {
                // somebody pressed the aim button;
                _log = File.AppendText("c:/temp/aim.log");
                _log.WriteLine("Time\tFrame\t\tOffset\t");
                _logstart = DateTime.Now;

                _aiming = true;
            }
            else if (!a && _aiming && (_lasttargetoffset == _targetoffset)) {
                // we're done aiming
                int cycles = (int) _smartdashboard.GetDouble("aimExCycles");
                int frames = (int) _smartdashboard.GetDouble("aimFrames");
                double tm = _smartdashboard.GetDouble("aimTime");

                if (_log != null) {
                    _log.WriteLine("{0}\tCycles\t{1}\tFrames\t{2}\tTime\t{3:0.00}", timestamp(), cycles, frames, tm);
                    _log.Flush();
                    _log.Close();
                    _log = null;
                }
                else
                    _aiming = false; // something got broken

                // done with aiming.
            }

            _lasttargetoffset = _targetoffset;
        }

        void adjustTrim() {
            double last = _robottrim;
            _robottrim = _smartdashboard.GetDouble("trim");

            if (last != _robottrim) {
                if (_robottrim > last) 
                    _trim++;
                else if (_robottrim < last) 
                    _trim--;

                Settings.Default.Trim = _trim;
                Settings.Default.Save();
                _robottrim = last;

            }
        }

        // aim targets the selected target, draws the linesh
        void aim() {
            RobotLocator rl = new RobotLocator(_particlefinder);
            string targetSelection = _smartdashboard.GetString("targetSelection");

            Particle target = null;

            switch (targetSelection) {
                case "left": 
                    target = rl.targetleft;
                    break;
                case "right": 
                    target = rl.targetright;
                    break;
                case "mid": 
                default:
                    target = rl.targetmid;
                    break;
            }
            
            adjustTrim();

            if (rl.targetmid != null) {
                _targetmid.Width = rl.targetmid.width;
                _targetmid.Height = rl.targetmid.height;
                Canvas.SetTop(_targetmid, rl.targetmid.top);
                Canvas.SetLeft(_targetmid, rl.targetmid.left);
            }
            else {
                _targetmid.Width = 0;
                _targetmid.Height = 0;
            }

            if (rl.targetleft != null) {
                _targetleft.Width = rl.targetleft.width;
                _targetleft.Height = rl.targetleft.height;
                Canvas.SetTop(_targetleft, rl.targetleft.top);
                Canvas.SetLeft(_targetleft, rl.targetleft.left);
            }
            else {
                _targetleft.Width = 0;
                _targetleft.Height = 0;
            }

            if (rl.targetright != null) {
                _targetright.Width = rl.targetright.width;
                _targetright.Height = rl.targetright.height;
                Canvas.SetTop(_targetright, rl.targetright.top);
                Canvas.SetLeft(_targetright, rl.targetright.left);
            }
            else {
                _targetright.Width = 0;
                _targetright.Height = 0;
            }

            int imgcenter = (rl.imgwidth / 2);
            double targetcenter = (target != null) ? target.centerx : 0;

            _targetoffset = (int)Math.Round(targetcenter - imgcenter);
            _centercamera.X1   = _centercamera.X2   = (imgcenter + _targetoffset);
            _centervertical.X1 = _centervertical.X2 = imgcenter - _trim;

            if (target != null) {
                _smartdashboard.SetDouble("offset", (double)_targetoffset);
                // _smartdashboard.SetDouble("width", (double)rl.targetmid.width);
                // _smartdashboard.SetDouble("height", (double)rl.targetmid.height);
            }
            else {
                // dang, no target
                _smartdashboard.SetDouble("offset", (double)320);
                // _smartdashboard.SetDouble("width", (double)0);
                // _smartdashboard.SetDouble("height", (double)0);
            }

            _smartdashboard.SetDouble("frameNum", (double)_framecount);

            string dir = string.Empty;

            if (_targetoffset > 0)
                dir = " Right";
            else if (_targetoffset < 0)
                dir = " Left";

            _direction.Text = _targetoffset.ToString() + dir;
        }

        bool processCameraImage() {
            if ((_camera != null) && (_camera.NewImage)) {
                byte[] img = _camera.Image;

                if (img != null) {
                    WriteFile(img, _tmppath);

                    BitmapImage bi = new BitmapImage();
                    using (MemoryStream m = new MemoryStream(img)) {
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = m;
                        bi.EndInit();
                    }

                    string pngpath = _particlefinder.ProcessPath(_tmppath);
                    // BitmapImage bi = new BitmapImage(new Uri(pngpath));

                    _image.Source = bi;

                    aim();
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
            aim();

            return true;
        }

        void dispatcherTimer_Tick(object sender, EventArgs e) {
             bool newimg = processCameraImage();

             if (newimg) {
                 if ((_framecount % 10) == 0) {
                     DateTime now = DateTime.Now;
                     TimeSpan delta = now - _lastframetime;
                     fps = (int)(10000.0 / delta.TotalMilliseconds);
                     _lastframetime = now;
                 }

                 _framecount++;

                 _report.Text = string.Format("frame {0} fps {1} trim {2} aiming {3}", _framecount, fps, _trim, _aiming);

                 // reportAiming();
             }

             if ((_camera != null) && _camera.CheckConnected())  {
                 _connected.Background = Brushes.Green;
                 _connected.Text = "connected";
             }
             else {
                 _connected.Background = Brushes.Red;
                 _connected.Text = "disconnected";
             }
        }

        // _settingsbtn_Click opens the settings window, and restarts the camera with the 
        // new settings.
        private void _settingsbtn_Click(object sender, RoutedEventArgs e) {
            if (_settings == null) {
                _settings = new AppSettings(this);

                _settings.Visibility = Visibility.Visible;

                if (_camera != null) {
                    _camera.Stop();
                    _camera = null;
                }
            }
        }
    }
}
