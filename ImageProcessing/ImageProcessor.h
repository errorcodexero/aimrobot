#ifndef IMAGEPROCESSING_TASK_INCLUDE
#define IMAGEPROCESSING_TASK_INCLUDE

#include <nivision.h>
#include <nimachinevision.h>

#define MAX_PARTICLES 128

extern "C" {
    void ReportError();
    void ErrorCheck(int val);
}

enum ColorPlane {
    Red = 1,
    Green = 2,
    Blue = 3
};

// Oh for a real programming environment...
#define PI (3.141592653589793) 

using namespace System;

// ImageProcessing encapsulates all of the vision processing code.
class ImageProcessor {
  private:
    bool _debug;
    int _luminence;    // Luminence threshold 

    double _aspratio_min; 
    double _aspratio_max; 
    double _cameraFOV;      // camera field of view
    double _minarea;        // filter out particles that are too small.

  public:
    // luminence: luminence threshold, 200 is a good number.
    // debug: output images written to outputdir
    ImageProcessor(int luminence, bool debug);

    int ExtractLuminancePlane(Image* image);
    int ExtractColorPlane(Image* image, ColorPlane  c);

    Image *ReadImage(char *path);
    void ReportImage(char *name, Image *img);
    Image* MakeImage(int width, int height);
    Image* MakeImage();
    void WriteImage(Image* img, const char *path);
    void WriteImage(Image* img, const char *path, int usepalette);

    int ProcessImage(Image *image);

    int numParticles;

    int imgwidth, imgheight;
};

#endif // ifndef IMAGEPROCESSING_TASK_INCLUDE

