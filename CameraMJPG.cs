using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Net;
using System.IO;
using System.Threading;
using System.Text;

namespace AimRobot {
    public class CameraMJPG {
        byte[] _image;
        Thread _t;
        bool _running = true;
        bool _newimage;
        int _wait;
        HttpWebRequest _request;
        HttpWebResponse _response;
        DateTime _lastframetime;

        // http://10.14.25.11/
        public const string httpmjpg = "axis-cgi/mjpg/video.cgi?resolution=640x480&fps=20&compression=60";
        public string baseurl = "http://10.14.25.11/";
        string _boundary = "--myboundary\r\n";

        public string fullurl;
        public string testurl = "http://localhost/video.jpg";

        void threadFn() {
            _lastframetime = DateTime.Now;

            while (_running) {
                // getMJPEGImage();

                if (!Connected && !Connecting) {
                    Connecting = true;
                    getMJPEGImageAsync();
                }

                Thread.Sleep(1000);
            }
        }

        string readLine(Stream stream) {
            using (StringWriter sw = new StringWriter()) {
                char last = (char) 0;

                while (true) {
                    char c = (char)stream.ReadByte();
                    sw.Write(c);

                    if (c == '\n' && last == '\r') {
                        return sw.ToString();
                    }

                    last = c;
                }
            }
        }

        void parseResponse() {
            try {
                HttpStatusCode status = _response.StatusCode;

                if (status == HttpStatusCode.OK) {
                    using (Stream respstr = _response.GetResponseStream()) {
                        // find boundary.
                        while (_running) {
                            while (true) {
                                string ln = readLine(respstr);
                                if (ln == _boundary)
                                    break;
                            }

                            string type = readLine(respstr);
                            if (type == "Content-Type: image/jpeg\r\n") {
                                string contentlen = readLine(respstr);
                                int col = contentlen.IndexOf(':');

                                if (col > 0) {
                                    readLine(respstr);  // skip over extra blank line...

                                    int size = Int32.Parse(contentlen.Substring(col + 1));

                                    byte[] bits = new byte[size];
                                    int offset = 0;
                                    int count = bits.Length;
                                    int r;

                                    while ((r = respstr.Read(bits, offset, count)) > 0) {
                                        offset += r;
                                        count -= r;
                                    }

                                    lock (this) {
                                        _newimage = true;
                                        _image = bits;
                                    }
                                }
                            }

                            _lastframetime = DateTime.Now;
                            Connected = true;
                        }
                    }
                }

                Message = "Disconnected";
                Connected = false;
                _response.Close();
            }
            catch (Exception e) {
                Message = e.Message;
                Connected = false;
            }
        }

        void responseCallback(IAsyncResult asynchronousResult) {
            try {
                Connecting = false;

                _request = (HttpWebRequest)asynchronousResult.AsyncState;
                _response = (HttpWebResponse)_request.GetResponse();

                Connected = true;

                parseResponse();
            }
            catch (Exception ex) {
                Message = ex.Message;
                Connected = false;
            }
        }

        void getMJPEGImageAsync() {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullurl);
                request.KeepAlive = true;
                request.ContentType = "image/jpeg";


                request.BeginGetResponse(new AsyncCallback(responseCallback), request); 
            }
            catch (Exception e) {
                Message = e.Message;
            }
            finally {
            }
        }

        void getMJPEGImage() {
            try {
                _request = (HttpWebRequest)WebRequest.Create(fullurl);
                _request.KeepAlive = true;
                _request.ContentType = "image/jpeg";

                _response = (HttpWebResponse) _request.GetResponse(); 

                parseResponse();
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally {
            }
        }

        ///////////////////////////////////////////////////////////////////////////
        //
        // publics
        //
        ///////////////////////////////////////////////////////////////////////////
        public CameraMJPG(string url, uint fps) {
            fullurl = url + httpmjpg;

            // fullurl = testurl;
            // _wait = (int) (1000 / fps);
            _wait = 1;
        }

        public void Start() {
            _t = new Thread(new ThreadStart(threadFn));
            _t.Start();
            _t.IsBackground = true;
        }

        public void Stop() {
            _running = false;

            try {
                _t.Abort();
                _t.Join();
            }
            catch {
            }
        }

        public byte[] Image { 
            get {
                lock (this) {
                    if (_image != null) {
                        _newimage = false;
                        return (byte[]) _image.Clone();
                    }
                    else
                        return null;
                }
            }
        }

        public bool NewImage {
            get { 
                lock (this)
                    return _newimage; 
            }
        }

        public bool CheckConnected() {
            TimeSpan diff = DateTime.Now - _lastframetime;

            if (diff.TotalSeconds > 10) {

                if (_response != null) {
                    try {
                        _response.Close();

                        Stream respstr = _response.GetResponseStream();
                        respstr.Close();

                        _request.Abort();
                    }
                    catch {
                    }

                    _request = null;
                    _response = null;
                    Connected = false;
                }
                return false;
            }
            else {
                return true;
            }
        }

        public bool Connected;
        public bool Connecting;

        public string Message;
    }
}
