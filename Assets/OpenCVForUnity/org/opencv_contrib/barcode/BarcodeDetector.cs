#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.BarcodeModule
{
    // C++: class BarcodeDetector


    public class BarcodeDetector : DisposableOpenCVObject
    {

        protected override void Dispose(bool disposing)
        {

            try
            {
                if (disposing)
                {
                }
                if (IsEnabledDispose)
                {
                    if (nativeObj != IntPtr.Zero)
                        barcode_BarcodeDetector_delete(nativeObj);
                    nativeObj = IntPtr.Zero;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }

        }

        protected internal BarcodeDetector(IntPtr addr) : base(addr) { }


        public IntPtr getNativeObjAddr() { return nativeObj; }

        // internal usage only
        public static BarcodeDetector __fromPtr__(IntPtr addr) { return new BarcodeDetector(addr); }

        //
        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector(string prototxt_path = "", string model_path = "")
        //

        /**
         * Initialize the BarcodeDetector.
         * param prototxt_path prototxt file path for the super resolution model
         * param model_path model file path for the super resolution model
         */
        public BarcodeDetector(string prototxt_path, string model_path)
        {


            nativeObj = DisposableObject.ThrowIfNullIntPtr(barcode_BarcodeDetector_BarcodeDetector_10(prototxt_path, model_path));


        }

        /**
         * Initialize the BarcodeDetector.
         * param prototxt_path prototxt file path for the super resolution model
         */
        public BarcodeDetector(string prototxt_path)
        {


            nativeObj = DisposableObject.ThrowIfNullIntPtr(barcode_BarcodeDetector_BarcodeDetector_11(prototxt_path));


        }

        /**
         * Initialize the BarcodeDetector.
         */
        public BarcodeDetector()
        {


            nativeObj = DisposableObject.ThrowIfNullIntPtr(barcode_BarcodeDetector_BarcodeDetector_12());


        }


        //
        // C++:  bool cv::barcode::BarcodeDetector::detect(Mat img, Mat& points)
        //

        /**
         * Detects Barcode in image and returns the rectangle(s) containing the code.
         *
         * param img grayscale or color (BGR) image containing (or not) Barcode.
         * param points Output vector of vector of vertices of the minimum-area rotated rectangle containing the codes.
         * For N detected barcodes, the dimensions of this array should be [N][4].
         * Order of four points in vector&lt; Point2f&gt; is bottomLeft, topLeft, topRight, bottomRight.
         * return automatically generated
         */
        public bool detect(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            return barcode_BarcodeDetector_detect_10(nativeObj, img.nativeObj, points.nativeObj);


        }


        //
        // C++:  bool cv::barcode::BarcodeDetector::decode(Mat img, Mat points, vector_string& decoded_info, vector_BarcodeType& decoded_type)
        //

        /**
         * Decodes barcode in image once it's found by the detect() method.
         *
         * param img grayscale or color (BGR) image containing bar code.
         * param points vector of rotated rectangle vertices found by detect() method (or some other algorithm).
         * For N detected barcodes, the dimensions of this array should be [N][4].
         * Order of four points in vector&lt;Point2f&gt; is bottomLeft, topLeft, topRight, bottomRight.
         * param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         * param decoded_type vector of BarcodeType, specifies the type of these barcodes
         * return automatically generated
         */
        public bool decode(Mat img, Mat points, List<string> decoded_info, List<int> decoded_type)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = barcode_BarcodeDetector_decode_10(nativeObj, img.nativeObj, points.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_BarcodeType(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }


        //
        // C++:  bool cv::barcode::BarcodeDetector::detectAndDecode(Mat img, vector_string& decoded_info, vector_BarcodeType& decoded_type, Mat& points = Mat())
        //

        /**
         * Both detects and decodes barcode
         *
         * param img grayscale or color (BGR) image containing barcode.
         * param decoded_info UTF8-encoded output vector of string(s) or empty vector of string if the codes cannot be decoded.
         * param decoded_type vector of BarcodeType, specifies the type of these barcodes
         * param points optional output vector of vertices of the found  barcode rectangle. Will be empty if not found.
         * return automatically generated
         */
        public bool detectAndDecode(Mat img, List<string> decoded_info, List<int> decoded_type, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = barcode_BarcodeDetector_detectAndDecode_10(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj, points.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_BarcodeType(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }

        /**
         * Both detects and decodes barcode
         *
         * param img grayscale or color (BGR) image containing barcode.
         * param decoded_info UTF8-encoded output vector of string(s) or empty vector of string if the codes cannot be decoded.
         * param decoded_type vector of BarcodeType, specifies the type of these barcodes
         * return automatically generated
         */
        public bool detectAndDecode(Mat img, List<string> decoded_info, List<int> decoded_type)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = barcode_BarcodeDetector_detectAndDecode_11(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_BarcodeType(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }


#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif



        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector(string prototxt_path = "", string model_path = "")
        [DllImport(LIBNAME)]
        private static extern IntPtr barcode_BarcodeDetector_BarcodeDetector_10(string prototxt_path, string model_path);
        [DllImport(LIBNAME)]
        private static extern IntPtr barcode_BarcodeDetector_BarcodeDetector_11(string prototxt_path);
        [DllImport(LIBNAME)]
        private static extern IntPtr barcode_BarcodeDetector_BarcodeDetector_12();

        // C++:  bool cv::barcode::BarcodeDetector::detect(Mat img, Mat& points)
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool barcode_BarcodeDetector_detect_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);

        // C++:  bool cv::barcode::BarcodeDetector::decode(Mat img, Mat points, vector_string& decoded_info, vector_BarcodeType& decoded_type)
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool barcode_BarcodeDetector_decode_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj);

        // C++:  bool cv::barcode::BarcodeDetector::detectAndDecode(Mat img, vector_string& decoded_info, vector_BarcodeType& decoded_type, Mat& points = Mat())
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool barcode_BarcodeDetector_detectAndDecode_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj, IntPtr points_nativeObj);
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool barcode_BarcodeDetector_detectAndDecode_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj);

        // native support for java finalize()
        [DllImport(LIBNAME)]
        private static extern void barcode_BarcodeDetector_delete(IntPtr nativeObj);

    }
}

#endif