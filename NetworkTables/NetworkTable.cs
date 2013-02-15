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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkTables {
    // NetworkTable represents a single "table" of named values.
    public class NetworkTable {
        NetworkTableConnection _connection;

        internal Dictionary<string, Entity> _entities = new Dictionary<string, Entity>();

        internal NetworkTable(NetworkTableConnection connection, string name) {
            _connection = connection;
            FullName = name;
        }

        internal void reset() {
            lock (this) {
                _entities.Clear();
            }
        }

        internal Entity findEntity(string name) {
            lock (this) {
                Entity e = null;

                if (_entities.ContainsKey(name))
                    e = _entities[name];

                return e;
            }
        }

        // GetString retrives the specified string value.
        public string GetString(string name) {
            Entity e = findEntity(name);

            if (e == null)
                return null;
            else
                return (string) e.value;
        }

        // GetDouble retrives the specified double value.
        public double GetDouble(string name) {
            Entity e = findEntity(name);

            if (e == null)
                return 0;
            else
                return (double) e.value;
        }

        // SetDouble writes a new value for the specified double.
        public void SetDouble(string name, double val) {
            Entity e = findEntity(name);

            if (e != null) {
                if (e.type != EntityType.Double)
                    throw new Exception("Entity " + e.fullpath + " is not a double.");

                if (((double) e.value) != val) {  // only write if 
                    e.value = val;
                    _connection.writeEntity(e);
                }
            }
        }

        public string FullName;
    }
}
