// NSWrap.h

#pragma once
#include <msclr\marshal.h>
#include "Particle.h"
#include "ImageProcessor.h"

#define MAX_PARTICLES 128


using namespace System;
using namespace System::Collections::Generic;

namespace Vision {
    public ref class ParticleFinder { 
        bool _debug;
        int _luminence;        // Luminence threshold 
        double _aspratio_min;  // minimum aspect ratio to be considered a target
        double _aspratio_max;  // maximum aspect ratio to be considerd atarget
        double _minarea;       // used to prune little particles early
        double _cameraFOV;     // camera field of view

        ImageProcessor *_ip;
        msclr::interop::marshal_context ^context;

        void VisionErrChk(int val) {
            // TODO: check val
        }

        int FindParticles(Image* image);

      public:
        List<Particle^>^ Particles;
        int imgwidth, imgheight;

        ParticleFinder(Byte luminance, bool debug, double minarea);

        String^ ProcessPath(String ^path);
    };
}
