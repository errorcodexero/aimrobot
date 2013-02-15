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
    public class FieldPoint {
        public double X;
        public double Y;
        public double Z;

        public FieldPoint() {
        }

        public FieldPoint(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }

        // Distance computes the distance from this FieldPoint to point p.
        public double Distance(FieldPoint p) {
            double x = (p.X - X);
            double y = (p.Y - Y);
            double z = (p.Z - Z);

            return Math.Sqrt(x * x + y * y + z * z);
        }

        // Angle computes the angel between left, right, and p, with p the
        // vertext of the angle.
        public double Angle(FieldPoint left, FieldPoint right, FieldPoint p) {
            double b = right.Distance(p);
            double a = left.Distance(p);
            double width = right.Distance(left);

            double C = Math.Acos((a * a + b * b - width * width) / (2 * a * b));

            // C = C * 180.0 / PI; // convert to degrees

            return C;
        }
    }

    // RobotPoint is a FieldPoint with extra information to help determine the 
    // position of the robot on the field.
    public class RobotPoint : FieldPoint {
        public double theta1;  // angle from the robot to the left edge of the left backboard
        // and the center of all backboards.
        public double theta2;  // angle from the robot to the right edge of the right backboard
        // and the center of all backboards.
        public double theta;   // theta1 + theta1, the total angle between left and right.
        public double alpha;   // the angle from the backboard, to the robot.
        public double beta;
        public double a;
        public double b;

        void init() {
            theta1 = 0;
            theta2 = 0;
            theta = 0;
            alpha = 0;
            beta = 0;
            a = 0;
            b = 0;
        }

        public RobotPoint() {
            init();
        }

        public RobotPoint(double x, double y, double z) : 
            base(x, y, z) {
            init();
        }
    }
}
