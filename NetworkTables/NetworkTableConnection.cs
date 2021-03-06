/*
 * NetworkTable is a basic implemntation of the FIRST Network Table protocol.
 *   http://firstforge.wpi.edu/sf/docman/do/downloadDocument/projects.wpilib/docman.root/doc1318
 *
 * Copyright (C) 2013, FIRST Robotics Team 1425 "Error Code Xero"
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkTables {
    // Network Tables have have only a few value types
    public enum EntityType {
        Boolean      = 0x00,
        Double       = 0x01,
        String       = 0x02,
        BooleanArray = 0x10,
        DoubleArray  = 0x11,
        StringArray  = 0x12 
    };

    // Network Tables have only a few message types
    public enum MessageType {
        KEEP_ALIVE_MESSAGE           = 0x00,
        CLIENT_HELLO_MESSAGE         = 0x01,
        PROTOCOL_VERSION_UNSUPPORTED = 0x02,
        SERVER_HELLO_COMPLETE        = 0x03,
        ENTRY_ASSIGNMENT_MESSAGE     = 0x10,
        ENTITY_FIELD_UPDATE          = 0x11,
    }

    // Entity represents a single Network Table value.
    // Entities are identified by what looks suspiciously like file paths, for example:
    //
    //     /SmartDashboard/Checkbox
    // 
    // is the "Checkbox" Entity of the "/SmartDashboard" table. Paths from an arbitrary hierachy,
    // and each '/' denotes another level and defines a "table".  
    //
    // Note that this implementation leaves in the '/' characters in the table paths, so they are required
    // to obtain a table via GetTable.
    public class Entity {
        public EntityType type;   // see EntityType
        public string tablepath;  // for the table, e.g., "/SmartDashboard"
        public string fullpath;   // for the entity, e.g., "/SmartDashboard/Checkbox"
        public string name;       // for the entity, e.g., "Checkbox"
        public ushort id;         // from ENTRY_ASSIGNMENT_MESSAGE
        public int sequence;      // incremented as the value changes
        public object value;      // the value has to go somehwere

        // Entity constructor, splits out the various paths/names
        public Entity(string fullname) {
            this.fullpath = fullname;

            int last = fullname.LastIndexOf('/');
            tablepath = fullname.Substring(0, last);
            name = fullname.Substring(last + 1, fullname.Length - last - 1);
        }
    }

    // NetworkTableConnection represents a connection to the Network Table protocol.  It's kept separate
    // from Network Table, since, honestly, it seems like a separate thing. 
    public class NetworkTableConnection {
        public string RobotHost = "10.14.25.2";  // the usual robot IP addr
        public int NetworkTablePort = 1735;      // the usual Network Table port

        TcpClient     _cRIOConnection;  // our friend, the TCP socket
        NetworkStream _nstream;         // the actual stream to send data
        Thread _thread;                 // used to read incoming messages
        bool _isrunning = true;         // 

        // _entities maps the protocol ID to the actual Entities.
        Dictionary<ushort, Entity> _entities = new Dictionary<ushort, Entity>();

        // _tables maps the indivdual table paths to the actual Network Tables.
        Dictionary<string, NetworkTable> _tables = new Dictionary<string, NetworkTable>();

        // readshort reads a 16 bit unsigned integer in "network byte order".
        ushort readushort() {
            ushort val = (ushort) (_nstream.ReadByte() << 8 | _nstream.ReadByte());
            return val;
        }

        // readString reads a protocol string value:
        //   len - 2 byte, unsigned, length of the string
        //   str - UTF-8 encoded characters for the string value
        string readString() {
            ushort len = readushort();
            byte[] strb = new byte[len];
            _nstream.Read(strb, 0, len);

            string str = new string(Encoding.UTF8.GetChars(strb));

            return str;
        }

        // readValue pulls of a single Entity of the specified type from the wire.
        object readValue(EntityType type) {
            object val = null;
            switch (type) {
                case EntityType.Boolean:
                    val = _nstream.ReadByte() == 0x01;
                    break;

                case EntityType.Double:
                    {
                        // doubles are encoded as 8 byte values, with this convenient conversion
                        long part = 0;
                        part += (long)(((long)_nstream.ReadByte()) << 56);
                        part += (long)(((long)_nstream.ReadByte()) << 48);
                        part += (long)(((long)_nstream.ReadByte()) << 40);
                        part += (long)(((long)_nstream.ReadByte()) << 32);
                        part += (long)(_nstream.ReadByte() << 24);
                        part += (long)(_nstream.ReadByte() << 16);
                        part += (long)(_nstream.ReadByte() << 8);
                        part += (long)(_nstream.ReadByte());
                        val = BitConverter.Int64BitsToDouble(part);
                        break;
                    }

                case EntityType.String:
                    val = readString();
                    break;

                case EntityType.BooleanArray:
                case EntityType.DoubleArray:
                case EntityType.StringArray:
                default:
                    // TODO: really should implement these
                    throw new Exception("readValue not implemented for EntityType " + type);
            }

            return val;
        }

        // readEntityAssignment implements the ENTRY_ASSIGNMENT_MESSAGE.
        // Field Name Field Type
        // 0x10                  - Entry Assignment Message 1 byte, unsigned
        // Entry Name            - string (actually the fully qulified "path)
        // Entry Type            - 1 byte, unsigned
        // Entry ID              - 2 byte, unsigned
        // Entry Sequence Number - 2 bytes, unsigned
        // Entry Value           - N bytes, length depends on Entry Type
        void readEntityAssignment() {
            string fullname = readString();

            Entity e = new Entity(fullname);

            e.type     = (EntityType) _nstream.ReadByte();
            e.id       = readushort();
            e.sequence = readushort();
            e.value    = readValue(e.type);

            Console.WriteLine("{0} id {1} - {2}", e.fullpath, e.id, e.value);

            _entities[e.id] = e;

            NetworkTable nt = GetTable(e.tablepath);
            nt._entities[e.name] = e;
        }

        // readEntityUpdate implements the ENTITY_FIELD_UPDATE message.
        void readEntityUpdate() {
            ushort id = readushort();
            ushort seqn = readushort();

            Entity e = _entities[id];

            object val = readValue(e.type);

            if (seqn > e.sequence)
                e.value = val;

            Console.WriteLine("{0} id {1} - {2}", e.fullpath, e.id, e.value);
        }

        // writeEntity writes out a new Entity value.  
        internal void writeEntity(Entity e) {
            try {
                e.sequence++;
                _nstream.WriteByte((byte)MessageType.ENTITY_FIELD_UPDATE);
                _nstream.WriteByte((byte)(e.id >> 8));
                _nstream.WriteByte((byte)(e.id));
                _nstream.WriteByte((byte)(e.sequence >> 8));
                _nstream.WriteByte((byte)(e.sequence));

                switch (e.type) {
                    case EntityType.Double: {
                            long lvalue = BitConverter.DoubleToInt64Bits((double)e.value);

                            long part = 0;
                            _nstream.WriteByte((byte)(lvalue >> 56));
                            _nstream.WriteByte((byte)(lvalue >> 48));
                            _nstream.WriteByte((byte)(lvalue >> 40));
                            _nstream.WriteByte((byte)(lvalue >> 32));
                            _nstream.WriteByte((byte)(lvalue >> 24));
                            _nstream.WriteByte((byte)(lvalue >> 16));
                            _nstream.WriteByte((byte)(lvalue >> 8));
                            _nstream.WriteByte((byte)(lvalue));
                            break;
                        }

                    case EntityType.Boolean:
                    case EntityType.String:
                    case EntityType.BooleanArray:
                    case EntityType.DoubleArray:
                    case EntityType.StringArray:
                    default:
                        // TODO: really should implement these
                        throw new Exception("writeEntity not implemented for EntityType " + e.type);
                }
            }
            catch (Exception ex) {
                StatusMsg = ex.Message;
                resetConnection();
            }
        }

        void resetConnection() {
            try {
                if (_nstream != null)
                    _nstream.Close();
                if (_cRIOConnection != null)
                    _cRIOConnection.Close();
            }
            catch {
            }

            _entities.Clear();
            foreach (NetworkTable t in _tables.Values) 
                t.reset();

            _nstream = null;
            _cRIOConnection = null;
        }

        // init cranks up the socket, and processes the SERVER_HELLO_COMPLETE message.
        void init() {
            // initialize socket
            _cRIOConnection = new TcpClient();
            _cRIOConnection.Connect(RobotHost, NetworkTablePort);
            _nstream = _cRIOConnection.GetStream();
            _cRIOConnection.NoDelay = true;

            // start things off
            HelloMessage hello = new HelloMessage();
            sendMessage(hello);
        }

        // sendMessage writes out the new message.  As it turns out, though, there is really only
        // one of these, HelloMessage, so this probably wasn't worth the trouble.
        void sendMessage(RequestMessage msg) {
            _nstream.Write(msg.Message, 0, msg.Message.Length);
        }

        // readLoop sits and listens to the socket, waiting for incoming messages.  There are actually
        // very few different message types.
        void readLoop() {
            while (_isrunning) {
                try {
                    if (_nstream == null) { // not connected
                        init();
                    }

                    MessageType b = (MessageType) _nstream.ReadByte();  // read message type

                    switch (b) {
                        case MessageType.ENTRY_ASSIGNMENT_MESSAGE:
                            readEntityAssignment();
                            break;
                        case MessageType.SERVER_HELLO_COMPLETE:
                            // initial values assigned
                            break;
                        case MessageType.ENTITY_FIELD_UPDATE:
                            readEntityUpdate();
                            break;
                        case MessageType.KEEP_ALIVE_MESSAGE:
                            break;
                        default:
                            // something bad happened
                            throw new Exception(string.Format("Response Message type {0} unsupported", b));
                    }
                }
                catch (Exception ex) {
                    StatusMsg = ex.Message;
                    resetConnection();
                }
            }
        }

        // Connect cranks up the socket, and processes the SERVER_HELLO_COMPLETE.
        public void Connect() {
            _thread = new Thread(new ThreadStart(this.readLoop));
            _thread.Name = "Network Tables";
            _thread.IsBackground = true;
            _thread.Start();
        }

        // GetTable obtains the requested table.  Note that the leading '/'s are required, while
        // the trailing '/' is not be specified.  So, it's "/LiveWindow/DriveBase/Front".  
        // Either retrieves an existing table, or makes a new one and returns that.
        public NetworkTable GetTable(string tablepath) {
            lock (this) {
                NetworkTable nt;

                // see if there's an existing table, or if we need a new table.
                if (_tables.ContainsKey(tablepath))
                    nt = _tables[tablepath];
                else {
                    nt = new NetworkTable(this, tablepath);
                    _tables[tablepath] = nt;
                }

                return nt;
            }
        }

        public string StatusMsg;

        public bool Connected { get { return _nstream != null;  } }
    }
}
