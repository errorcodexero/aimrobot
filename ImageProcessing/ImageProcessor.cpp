#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <nivision.h>
#include <nimachinevision.h>
#include <windows.h>

#include "ImageProcessor.h"


#define IVA_MAX_BUFFERS 10
#define IVA_STORE_RESULT_NAMES

#define TMPBUFSIZE 255

#define VisionErrChk(Function) {if (!(Function)) {success = 0; ReportError(); goto Error;}}

///////////////////////////////////////////////////////////////////////////
// random C functions.

void ErrorCheck(int val) {
    if (!val) {
        char *err = imaqGetErrorText(imaqGetLastError());
        printf(err);

        imaqDispose(err);
    }
}

void ReportError() {
    ErrorCheck(false);
}

///////////////////////////////////////////////////////////////////////////
//
// publics
//
///////////////////////////////////////////////////////////////////////////

ImageProcessor::ImageProcessor(int luminence, bool debug) : 
    _luminence(luminence),
    _debug(debug),
    _aspratio_min(.25),
    _aspratio_max(5.0)
{
}

void ImageProcessor::WriteImage(Image* img, const char *path, int usepalette)
{
    // GACK...
    // char path[TMPBUFSIZE];
    // TODO: check string fits... 
    // sprintf(path, "%s%s", _outputdir, file);

    //int width, height;
    //imaqGetImageSize(img, &width, &height);
    // printf("Writing file %s, width %d heigh %d\n", path, width, height);

    if (usepalette) {
        RGBValue colorTable[256];
        // Priv_SetWriteFileAllowed(1);
        memset(colorTable, 0, sizeof(colorTable));
        colorTable[0].R = 0;
        colorTable[1].R = 255;
        colorTable[0].G = colorTable[1].G = 0;
        colorTable[0].B = colorTable[1].B = 0;
        colorTable[0].alpha = colorTable[1].alpha = 0;

        ErrorCheck(imaqWriteFile(img, path, colorTable));
    }
    else
        ErrorCheck(imaqWriteFile(img, path, NULL));
}

void ImageProcessor::WriteImage(Image* img, const char *path)
{
    WriteImage(img, path, FALSE);
}

Image* ImageProcessor::MakeImage() {
    // Image* image = imaqCreateImage(IMAQ_IMAGE_U8, 0);
    // Image* image = imaqCreateImage(IMAQ_IMAGE_HSL, DEFAULT_BORDER_SIZE); 
    Image* image = imaqCreateImage(IMAQ_IMAGE_RGB, DEFAULT_BORDER_SIZE); 

    if (!image) {
        ReportError();
        imaqDispose(image);
        return NULL;
    }
    
    return image;
}

// MakeImagae makes a new 
Image* ImageProcessor::MakeImage(int width, int height) {
    Image* image = MakeImage();

    if (!imaqSetImageSize(image, width, height)) {
        ReportError();
        imaqDispose(image);
        return NULL;
    }
    
    return image;
}

// ReadImage reads an Image from a file uisng imaqReadFile.
Image *ImageProcessor::ReadImage(char *path) {
    //-----------------------------------------------------------------------
    //  Create a new image.  Since pattern matching works only on 8 bit images
    //  we'll make an 8 bit image.
    //-----------------------------------------------------------------------
    Image* image = MakeImage(); 

    ErrorCheck(imaqReadFile(image, path, NULL, NULL));

    return image;
}

////////////////////////////////////////////////////////////////////////////////
//
// Function Name: ExtractLuminancePlane
// Description  : Extracts the luminance plane from a color image.
// Parameters   : image  - Input image
// Return Value : 1 == success
//
////////////////////////////////////////////////////////////////////////////////
int ImageProcessor::ExtractLuminancePlane(Image* image)
{
    int success = 1;
    Image* plane = imaqCreateImage(IMAQ_IMAGE_U8, 7);

    VisionErrChk(imaqExtractColorPlanes(image, IMAQ_HSL, NULL, NULL, plane));  // Extracts the luminance plane

    // Overwrite the original image with the Luminance plane.
    VisionErrChk(imaqDuplicate(image, plane));
    VisionErrChk(imaqDispose(plane));

Error:
    ErrorCheck(success);
    
    return success;
}

int ImageProcessor::ExtractColorPlane(Image* image, ColorPlane  c)
{
    int success = 1;
    Image* plane = imaqCreateImage(IMAQ_IMAGE_U8, 7);

    if (c == ColorPlane::Red) {
        VisionErrChk(imaqExtractColorPlanes(image, IMAQ_RGB, plane, NULL, NULL));  // Extracts the red plane
    }
    else if (c == ColorPlane::Green) {
        VisionErrChk(imaqExtractColorPlanes(image, IMAQ_RGB, NULL, plane, NULL));  // Extracts the red plane
    }
    else if (c == ColorPlane::Blue) {
        VisionErrChk(imaqExtractColorPlanes(image, IMAQ_RGB, NULL, NULL, plane));  // Extracts the red plane
    }

    // Overwrite the original image with the Luminance plane.
    VisionErrChk(imaqDuplicate(image, plane));

    VisionErrChk(imaqDispose(plane));

Error:
    ErrorCheck(success);
    
    return success;
}

///////////////////////////////////////////////////////////////////////////
//
// VisionProcessImage performs the following transformation on image:
//
// {1} - Extract the HSL luminance plane.
// {2} - perform a threshold transformation, picking out the "bright" areas.
// {3} - eliminate particles touching the border of the image.
// {4} - perform a "convex hull" transformation, to fill in the bright areas.
// {5} - filter out "small" particals.  
// 
///////////////////////////////////////////////////////////////////////////

int ImageProcessor::ProcessImage(Image *image)
{
    int success = 1;

    imaqGetImageSize(image, &imgwidth, &imgheight);

    // VisionErrChk(ExtractLuminancePlane(image));                      // {1}
    //    if (_debug)
    //      WriteImage(image, "Luminance.png", false);
    VisionErrChk(ExtractColorPlane(image, ColorPlane::Blue));
    if (_debug)
        WriteImage(image, "Blue.png", false);

    VisionErrChk(imaqThreshold(image, image, _luminence, 255, TRUE, 255));  // {2}
    if (_debug)
        WriteImage(image, "Threshold.png", false);

    // VisionErrChk(imaqRejectBorder(image, image, FALSE));             // {3}

    VisionErrChk(imaqConvexHull(image, image, FALSE));               // {4}
    if (_debug)
        WriteImage(image, "ConvexHull.png", TRUE);

    VisionErrChk(imaqSizeFilter(image, image, TRUE, 2, IMAQ_KEEP_LARGE, NULL)); // {5} 

    if (_debug)
        WriteImage(image, "SizeFilter.png", TRUE);

    // VisionErrChk(IVA_Particle(image, FALSE, pPixelMeasurements, 4, pCalibratedMeasurements, 0, ivaData, 5));

Error:
    return success;
}



