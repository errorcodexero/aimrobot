#include <msclr\marshal.h>
using namespace msclr::interop;

#include <stdlib.h>
#include <string.h>

#include "ParticleFinder.h"

namespace Vision {
    ParticleFinder::ParticleFinder(Byte luminance, bool debug, double minarea) :
        _aspratio_min(1.5),  
        _aspratio_max(6.0),
        _cameraFOV(45.0) 
    {
        context = gcnew msclr::interop::marshal_context();
        _ip = new ImageProcessor(luminance, debug);
        Particles = gcnew List<Particle^>();
        _minarea = minarea;
    }

    // ProcessPath reads the image from path, and finds the particles.
    String^ ParticleFinder::ProcessPath(String ^path) {
        String ^png = path->Replace("jpg", "png");
        const char* spath = context->marshal_as<const char*>(path);  // read as .jpg
        const char* ppath = context->marshal_as<const char *>(png);  // need to save as .png

        Image *img = _ip->ReadImage((char *) spath);
        _ip->ProcessImage(img);
        _ip->WriteImage(img, ppath, true);

        int n = FindParticles(img);

        imgwidth = _ip->imgwidth;
        imgheight = _ip->imgheight;

        imaqDispose(img);

        return png;
    }

    // FindParticles extracts the inividual particles from image,
    // putting them in Particles.
    int ParticleFinder::FindParticles(Image* image)
    {
        int success = 1;

        int numParticles;
        VisionErrChk(imaqCountParticles(image, TRUE, &numParticles));

        // Only process MAX_PARTICLES...
        if (numParticles > MAX_PARTICLES)
            numParticles = MAX_PARTICLES;

        Particles->Clear();

        int foundParticles = 0;
        for (int i = 0; i < numParticles; i++) {
            Particle ^p = gcnew Particle();

            p->ReadParticle(image, i);

            double aratio = p->width / p->height;
            if ((aratio > _aspratio_min) && 
                (aratio < _aspratio_max) && 
                ((p->width * p->height) > _minarea)
                ) {
                Particles->Add(p);
            }
        }

        return success;
    }
}
