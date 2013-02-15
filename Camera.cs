using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Net;
using System.IO;
using System.Threading;

namespace AimRobot {
    public class Camera {
        byte[] _image;
        Thread _t;
        bool _running = true;
        bool _newimage;
        int _wait;

        public const string httpjpg = "jpg/image.jpg";
        public string baseurl = "http://10.14.25.11/";

        public string fullurl;
        // public string testurl = "http://localhost/fake-img1.png";
        // public string testurl = "http://localhost/11-0-straight.jpg";
        // public string testurl = "http://localhost/img2.png";

        void responseCallback(IAsyncResult asynchronousResult) {  
            _image = null;

            HttpWebRequest request = (HttpWebRequest) asynchronousResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            try {
                HttpStatusCode status = response.StatusCode;

                if (status == HttpStatusCode.OK) {
                    using (Stream respstr = response.GetResponseStream()) {
                        byte[] bits = new byte[response.ContentLength];
                        int offset = 0;
                        int count = bits.Length;
                        int r;

                        while ((r = respstr.Read(bits, offset, count)) > 0) {
                            offset += r;
                            count -= r;
                        }

                        _image = bits;
                    }
                }

            }
            catch {
            }
            finally {
                response.Close();
            }
        }

        void threadFn() {
            while (_running) {
                GetImage();

                // Thread.Sleep(_wait);
            }
        }

        void GetImageAsync() {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(fullurl);
            request.KeepAlive = false;
            request.ContentType = "image/jpeg";

            request.BeginGetResponse(new AsyncCallback(responseCallback), request); 
        }

        void GetImage() {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullurl);
                request.KeepAlive = false;
                request.ContentType = "image/jpeg";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse(); 

                HttpStatusCode status = response.StatusCode;

                if (status == HttpStatusCode.OK) {
                    using (Stream respstr = response.GetResponseStream()) {
                        byte[] bits = new byte[response.ContentLength];
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

                response.Close();
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
        public Camera(string url, uint fps) {
            fullurl = url + httpjpg; // +"?resolution=320x240"; // +"?compression=25";

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
    }
}
