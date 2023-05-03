#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.BarcodeModule
{
    // C++: class Barcode


    public class Barcode
    {

        // C++: enum cv.barcode.BarcodeType
        public const int NONE = 0;
        public const int EAN_8 = 1;
        public const int EAN_13 = 2;
        public const int UPC_A = 3;
        public const int UPC_E = 4;
        public const int UPC_EAN_EXTENSION = 5;


    }
}

#endif
