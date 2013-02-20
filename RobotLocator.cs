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
        public int imgwidth, imgheight, center;

        double midaspectmax = .385; // (10.0in / 31.0in) + 10% for error
        double midaspectmin = .29; // (10.0in / 31.0in) - 10% for error

        double sideaspectmax = .61; // (29.0in / 62.0in) + 10% for error
        double sideaspectmin = .386; // (29.0in / 62.0in) - 10% for error

        public static int compareParticleSize(Particle p1,
                                              Particle p2) {
            double val = p2.area - p1.area;

            if (val > 0)
                return 1;
            else if (val < 0)
                return -1;
            else return 0;
        }

        public static int compareParticleLeft(Particle p1,
                                              Particle p2) {
            double val = (p1.left - p2.left);

            if (val > 0)
                return 1;
            else if (val < 0)
                return -1;
            else return 0;
        }

        void findTargets() {
            _particles.Sort(compareParticleSize);

            // only take the top three...  Gack.
            int count = 3;
            if (_particles.Count < 3)
                count = _particles.Count;

            List<Particle> main = new List<Particle>();
            for (int i = 0; i < count; i++ )
                main.Add(_particles[i]);

            main.Sort(compareParticleLeft);

            _particles = main;
        }

        void findByAspectRatio(ParticleFinder pi) {
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

        }

        ///////////////////////////////////////////////////////////////////////////
        // 
        public RobotLocator(ParticleFinder pi) {
            _particles = pi.Particles;

            imgheight = pi.imgheight;
            imgwidth = pi.imgwidth;

            findTargets();

            if (_particles.Count == 3) {
                targetleft = _particles[0];
                targetmid = _particles[1];
                targetright = _particles[2];
            }
            else if (_particles.Count == 2) {
                Particle p1 = _particles[0];
                Particle p2 = _particles[1];

                int center = imgwidth / 2;

                if (p1.area > p2.area) {
                   targetleft = p1;
                   targetmid = p2;
                }
                else {
                    targetmid = p1;
                    targetright = p2;
                }
            }
            else if (_particles.Count == 1) {
                targetmid = _particles[0];
            }
        }
    }
}
