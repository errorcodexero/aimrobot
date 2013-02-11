using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkTables {
    public abstract class RequestMessage {
        public virtual void Write(byte[] msg) {
        }

        public abstract byte[] Message { get; }
    }

    public class HelloMessage : RequestMessage
    {
        public override byte[] Message {
            get {
                byte[] hello = { 0x01, 0x02, 0x00 }; // ask for protocol 2

                return hello;
            }
        }
    }
}
