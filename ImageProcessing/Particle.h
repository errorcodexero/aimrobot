// NSWrap.h

#pragma once
#include <msclr\marshal.h>
#include "ImageProcessor.h"

#define MAX_PARTICLES 128


using namespace System;
using namespace System::Collections::Generic;

namespace Vision {
    public ref class Particle {
        void VisionErrChk(int val) {
            // TODO: check val
        }

      public:
        System::Double left, top, width, height, right, bottom, centerx, centery, area;

        Particle() {
        }

        void ReadParticle(Image *image, int particleNumber) {
            int success;
    
            double left, top, width, height, centerx, centery;
        
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_BOUNDING_RECT_LEFT, &left));
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_BOUNDING_RECT_TOP, &top));
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_BOUNDING_RECT_WIDTH, &width));
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_BOUNDING_RECT_HEIGHT, &height));
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_CENTER_OF_MASS_X, &centerx));
            VisionErrChk(imaqMeasureParticle(image, particleNumber, false, IMAQ_MT_CENTER_OF_MASS_Y, &centery));

            this->left = left;
            this->top = top;
            this->width = width;
            this->height = height;
            this->centerx = centerx;
            this->centery = centery;
            this->bottom = top + height;
            this->right = left + width;
            this->area = width * height;

          Error:
            return;
        }

        bool IsAbove(Particle ^p) {
            return top > p->top;
        }

        bool IsLeft(Particle ^p) {
            return left > p->left;
        }
    };
}
