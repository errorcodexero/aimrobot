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
    // RobotLocator tries to find out the targets goals
    public class RobotLocator {
        public double cameraFOV = 45.0;      // camera field of view

        List<Particle> _particles;
        public Particle targetmid;  // middle goal
        public Particle targetleft; // left goal
        public Particle targetright; // right goal

        public int imgwidth, imgheight;
        public int horizontaloffset;
        public double targetcenter;

        double midaspectmax = .385; // (10.0in / 31.0in) + 10% for error
        double midaspectmin = .29; // (10.0in / 31.0in) - 10% for error

        double sideaspectmax = .6; // (29.0in / 62.0in) + 10% for error
        double sideaspectmin = .386; // (29.0in / 62.0in) - 10% for error

        ///////////////////////////////////////////////////////////////////////////
        // 
        public RobotLocator(ParticleFinder pi) {
            _particles = pi.Particles;
            imgheight = pi.imgheight;
            imgwidth = pi.imgwidth;

            // look for mid goal, find the biggest particle with the right aspect ratio
            foreach (Particle p in pi.Particles) {
                if ((p.aspectratio > midaspectmin) && (p.aspectratio < midaspectmax)) {
                    if ((targetmid == null) || (p.area > targetmid.area))
                        targetmid = p;
                }
            }

            // look for side goals
            if (targetmid != null) {
                foreach (Particle p in pi.Particles) {
                    if ((p.aspectratio > sideaspectmin) && (p.aspectratio < sideaspectmax)) {
                        // we've got a side particle
                        if (targetmid.right < p.left) {
                            if ((targetright == null) || (p.area > targetright.area))
                                targetright = p;  // take the biggest one
                        }
                        else if (targetmid.left > p.right) {
                            if ((targetleft == null) || (p.area > targetleft.area))
                                targetleft = p;   // take the biggest one
                        }
                    }
                }
            }
            else {
                // if we couldn't find anything above, look for the largest particle
                foreach (Particle p in pi.Particles) {
                    if ((targetmid == null) || (p.area > targetmid.area)) {
                        targetmid = p;
                    }
                }
            }

            if (targetmid != null)
                targetcenter = targetmid.centerx;
            else
                targetcenter = 0;

            horizontaloffset = ((int) Math.Round(targetcenter)) - (imgwidth / 2);
        }
    }
}
