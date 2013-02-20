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
        public Particle targetmid;  // middle goal
        public Particle targetleft; // left goal
        public Particle targetright; // right goal

        public int imgwidth, imgheight, center;

        public double distance;

        double midaspectmax = .385; // (10.0in / 31.0in) + 10% for error
        double midaspectmin = .29; // (10.0in / 31.0in) - 10% for error

        double sideaspectmax = .61; // (29.0in / 62.0in) + 10% for error
        double sideaspectmin = .386; // (29.0in / 62.0in) - 10% for error

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
            targetmid = null;

            // find the biggest particle that crosses the center.
            foreach (Particle p in pi.Particles) {
                double aspectParticle = (p.height / p.width);
                if ((aspectParticle > midaspectmin) && (aspectParticle < midaspectmax)) 
                {
                    targetmid = p;
                    break;
                }
            }
            if (targetmid != null)
            {
                foreach (Particle p in pi.Particles)
                {
                    double aspectParticle = (p.height / p.width);
                    if ((aspectParticle > sideaspectmin) && (aspectParticle < sideaspectmax) && (targetmid.right < p.left))
                    {
                        targetright = p;
                        break;
                    }
                }
                foreach (Particle p in pi.Particles)
                {
                    double aspectParticle = (p.height / p.width);
                    if ((aspectParticle > sideaspectmin) && (aspectParticle < sideaspectmax) && (targetmid.left > p.right))
                    {
                        targetleft = p;
                        break;
                    }
                }
                //if ((p.left < center) && (p.right > center) && 
                //    ((target == null) || (p.area > target.area))) {
                //    target = p;
            }
                if (targetmid == null)
                {
                    // find largest particle
                    foreach (Particle p in pi.Particles)
                    {
                        if ((targetmid == null) || (p.area > targetmid.area))
                        {
                            targetmid = p;
                        }
                    }
                }
          
        }
    }
}
