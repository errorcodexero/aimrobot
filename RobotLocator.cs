using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Net;
using System.IO;
using System.Threading;

using Vision;

namespace AimRobot {
    // RobotLocator tries to figure out the location of the Robot on the field
    // relative to the target goal.
    public class RobotLocator {
        // aspect ratio tolerance, to check for proper 4:3 ratio
        public double cameraFOV = 45.0;      // camera field of view

        List<Particle> _particles;
        public Particle target;  // goals

        public int imgwidth, imgheight, center;
        public int horizontaloffset;
        public double targetcenter;

        public double distance;

        double DegreesToRadians(double angle) {
            return angle * (Math.PI / 180.0);
        }

        double RadiansToDegrees(double angle) {
            return angle * (180.0 / Math.PI);
        }

        double sind(double degrees) {
            return Math.Sin(DegreesToRadians(degrees));
        }

        double cosd(double degrees) {
            return Math.Cos(DegreesToRadians(degrees));
        }

        ///////////////////////////////////////////////////////////////////////////
        // 
        public RobotLocator(ParticleFinder pi) {
            _particles = pi.Particles;
            imgheight = pi.imgheight;
            imgwidth = pi.imgwidth;

            center = imgwidth / 2;  // best guess if we can't figure it out for sure.
            target = null;

            // find the biggest particle that crosses the center.
            foreach (Particle p in pi.Particles) {
                if ((p.left < center) && (p.right > center) && 
                    ((target == null) || (p.area > target.area))) {
                    target = p;
                }
            }

            if (target == null) {
                // find largest particle
                foreach (Particle p in pi.Particles) {
                    if ((target == null) || (p.area > target.area)) {
                        target = p;
                    }
                }
            }

            if (target != null)
                targetcenter = target.centerx;
            else
                targetcenter = 0;

            horizontaloffset = ((int) Math.Round(targetcenter)) - (imgwidth / 2);
        }
    }
}
