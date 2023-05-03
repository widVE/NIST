

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.ObjdetectModule
{
    // C++: class QRCodeDetector


    public class QRCodeDetector : DisposableOpenCVObject
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
                        objdetect_QRCodeDetector_delete(nativeObj);
                    nativeObj = IntPtr.Zero;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }

        }

        protected internal QRCodeDetector(IntPtr addr) : base(addr) { }


        public IntPtr getNativeObjAddr() { return nativeObj; }

        // internal usage only
        public static QRCodeDetector __fromPtr__(IntPtr addr) { return new QRCodeDetector(addr); }

        //
        // C++:   cv::QRCodeDetector::QRCodeDetector()
        //

        public QRCodeDetector()
        {


            nativeObj = DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_QRCodeDetector_10());


        }


        //
        // C++:  void cv::QRCodeDetector::setEpsX(double epsX)
        //

        /**
         * sets the epsilon used during the horizontal scan of QR code stop marker detection.
         *      param epsX Epsilon neighborhood, which allows you to determine the horizontal pattern
         *      of the scheme 1:1:3:1:1 according to QR code standard.
         */
        public void setEpsX(double epsX)
        {
            ThrowIfDisposed();

            objdetect_QRCodeDetector_setEpsX_10(nativeObj, epsX);


        }


        //
        // C++:  void cv::QRCodeDetector::setEpsY(double epsY)
        //

        /**
         * sets the epsilon used during the vertical scan of QR code stop marker detection.
         *      param epsY Epsilon neighborhood, which allows you to determine the vertical pattern
         *      of the scheme 1:1:3:1:1 according to QR code standard.
         */
        public void setEpsY(double epsY)
        {
            ThrowIfDisposed();

            objdetect_QRCodeDetector_setEpsY_10(nativeObj, epsY);


        }


        //
        // C++:  void cv::QRCodeDetector::setUseAlignmentMarkers(bool useAlignmentMarkers)
        //

        /**
         * use markers to improve the position of the corners of the QR code
         *
         * alignmentMarkers using by default
         * param useAlignmentMarkers automatically generated
         */
        public void setUseAlignmentMarkers(bool useAlignmentMarkers)
        {
            ThrowIfDisposed();

            objdetect_QRCodeDetector_setUseAlignmentMarkers_10(nativeObj, useAlignmentMarkers);


        }


        //
        // C++:  bool cv::QRCodeDetector::detect(Mat img, Mat& points)
        //

        /**
         * Detects QR code in image and returns the quadrangle containing the code.
         *      param img grayscale or color (BGR) image containing (or not) QR code.
         *      param points Output vector of vertices of the minimum-area quadrangle containing the code.
         * return automatically generated
         */
        public bool detect(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            return objdetect_QRCodeDetector_detect_10(nativeObj, img.nativeObj, points.nativeObj);


        }


        //
        // C++:  string cv::QRCodeDetector::decode(Mat img, Mat points, Mat& straight_qrcode = Mat())
        //

        /**
         * Decodes QR code in image once it's found by the detect() method.
         *
         *      Returns UTF8-encoded output string or empty string if the code cannot be decoded.
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points Quadrangle vertices found by detect() method (or some other algorithm).
         *      param straight_qrcode The optional output image containing rectified and binarized QR code
         * return automatically generated
         */
        public string decode(Mat img, Mat points, Mat straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            if (straight_qrcode != null) straight_qrcode.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_decode_10(nativeObj, img.nativeObj, points.nativeObj, straight_qrcode.nativeObj)));

            return retVal;
        }

        /**
         * Decodes QR code in image once it's found by the detect() method.
         *
         *      Returns UTF8-encoded output string or empty string if the code cannot be decoded.
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points Quadrangle vertices found by detect() method (or some other algorithm).
         * return automatically generated
         */
        public string decode(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_decode_11(nativeObj, img.nativeObj, points.nativeObj)));

            return retVal;
        }


        //
        // C++:  String cv::QRCodeDetector::decodeCurved(Mat img, Mat points, Mat& straight_qrcode = Mat())
        //

        /**
         * Decodes QR code on a curved surface in image once it's found by the detect() method.
         *
         *      Returns UTF8-encoded output string or empty string if the code cannot be decoded.
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points Quadrangle vertices found by detect() method (or some other algorithm).
         *      param straight_qrcode The optional output image containing rectified and binarized QR code
         * return automatically generated
         */
        public string decodeCurved(Mat img, Mat points, Mat straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            if (straight_qrcode != null) straight_qrcode.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_decodeCurved_10(nativeObj, img.nativeObj, points.nativeObj, straight_qrcode.nativeObj)));

            return retVal;
        }

        /**
         * Decodes QR code on a curved surface in image once it's found by the detect() method.
         *
         *      Returns UTF8-encoded output string or empty string if the code cannot be decoded.
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points Quadrangle vertices found by detect() method (or some other algorithm).
         * return automatically generated
         */
        public string decodeCurved(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_decodeCurved_11(nativeObj, img.nativeObj, points.nativeObj)));

            return retVal;
        }


        //
        // C++:  string cv::QRCodeDetector::detectAndDecode(Mat img, Mat& points = Mat(), Mat& straight_qrcode = Mat())
        //

        /**
         * Both detects and decodes QR code
         *
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points optional output array of vertices of the found QR code quadrangle. Will be empty if not found.
         *      param straight_qrcode The optional output image containing rectified and binarized QR code
         * return automatically generated
         */
        public string detectAndDecode(Mat img, Mat points, Mat straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            if (straight_qrcode != null) straight_qrcode.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecode_10(nativeObj, img.nativeObj, points.nativeObj, straight_qrcode.nativeObj)));

            return retVal;
        }

        /**
         * Both detects and decodes QR code
         *
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points optional output array of vertices of the found QR code quadrangle. Will be empty if not found.
         * return automatically generated
         */
        public string detectAndDecode(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecode_11(nativeObj, img.nativeObj, points.nativeObj)));

            return retVal;
        }

        /**
         * Both detects and decodes QR code
         *
         *      param img grayscale or color (BGR) image containing QR code.
         * return automatically generated
         */
        public string detectAndDecode(Mat img)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecode_12(nativeObj, img.nativeObj)));

            return retVal;
        }


        //
        // C++:  string cv::QRCodeDetector::detectAndDecodeCurved(Mat img, Mat& points = Mat(), Mat& straight_qrcode = Mat())
        //

        /**
         * Both detects and decodes QR code on a curved surface
         *
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points optional output array of vertices of the found QR code quadrangle. Will be empty if not found.
         *      param straight_qrcode The optional output image containing rectified and binarized QR code
         * return automatically generated
         */
        public string detectAndDecodeCurved(Mat img, Mat points, Mat straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            if (straight_qrcode != null) straight_qrcode.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecodeCurved_10(nativeObj, img.nativeObj, points.nativeObj, straight_qrcode.nativeObj)));

            return retVal;
        }

        /**
         * Both detects and decodes QR code on a curved surface
         *
         *      param img grayscale or color (BGR) image containing QR code.
         *      param points optional output array of vertices of the found QR code quadrangle. Will be empty if not found.
         * return automatically generated
         */
        public string detectAndDecodeCurved(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecodeCurved_11(nativeObj, img.nativeObj, points.nativeObj)));

            return retVal;
        }

        /**
         * Both detects and decodes QR code on a curved surface
         *
         *      param img grayscale or color (BGR) image containing QR code.
         * return automatically generated
         */
        public string detectAndDecodeCurved(Mat img)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();

            string retVal = Marshal.PtrToStringAnsi(DisposableObject.ThrowIfNullIntPtr(objdetect_QRCodeDetector_detectAndDecodeCurved_12(nativeObj, img.nativeObj)));

            return retVal;
        }


        //
        // C++:  bool cv::QRCodeDetector::detectMulti(Mat img, Mat& points)
        //

        /**
         * Detects QR codes in image and returns the vector of the quadrangles containing the codes.
         *      param img grayscale or color (BGR) image containing (or not) QR codes.
         *      param points Output vector of vector of vertices of the minimum-area quadrangle containing the codes.
         * return automatically generated
         */
        public bool detectMulti(Mat img, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();

            return objdetect_QRCodeDetector_detectMulti_10(nativeObj, img.nativeObj, points.nativeObj);


        }


        //
        // C++:  bool cv::QRCodeDetector::decodeMulti(Mat img, Mat points, vector_string& decoded_info, vector_Mat& straight_qrcode = vector_Mat())
        //

        /**
         * Decodes QR codes in image once it's found by the detect() method.
         *      param img grayscale or color (BGR) image containing QR codes.
         *      param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         *      param points vector of Quadrangle vertices found by detect() method (or some other algorithm).
         *      param straight_qrcode The optional output vector of images containing rectified and binarized QR codes
         * return automatically generated
         */
        public bool decodeMulti(Mat img, Mat points, List<string> decoded_info, List<Mat> straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat straight_qrcode_mat = new Mat();
            bool retVal = objdetect_QRCodeDetector_decodeMulti_10(nativeObj, img.nativeObj, points.nativeObj, decoded_info_mat.nativeObj, straight_qrcode_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_Mat(straight_qrcode_mat, straight_qrcode);
            straight_qrcode_mat.release();
            return retVal;
        }

        /**
         * Decodes QR codes in image once it's found by the detect() method.
         *      param img grayscale or color (BGR) image containing QR codes.
         *      param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         *      param points vector of Quadrangle vertices found by detect() method (or some other algorithm).
         * return automatically generated
         */
        public bool decodeMulti(Mat img, Mat points, List<string> decoded_info)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            bool retVal = objdetect_QRCodeDetector_decodeMulti_11(nativeObj, img.nativeObj, points.nativeObj, decoded_info_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            return retVal;
        }


        //
        // C++:  bool cv::QRCodeDetector::detectAndDecodeMulti(Mat img, vector_string& decoded_info, Mat& points = Mat(), vector_Mat& straight_qrcode = vector_Mat())
        //

        /**
         * Both detects and decodes QR codes
         *     param img grayscale or color (BGR) image containing QR codes.
         *     param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         *     param points optional output vector of vertices of the found QR code quadrangles. Will be empty if not found.
         *     param straight_qrcode The optional output vector of images containing rectified and binarized QR codes
         * return automatically generated
         */
        public bool detectAndDecodeMulti(Mat img, List<string> decoded_info, Mat points, List<Mat> straight_qrcode)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat straight_qrcode_mat = new Mat();
            bool retVal = objdetect_QRCodeDetector_detectAndDecodeMulti_10(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, points.nativeObj, straight_qrcode_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_Mat(straight_qrcode_mat, straight_qrcode);
            straight_qrcode_mat.release();
            return retVal;
        }

        /**
         * Both detects and decodes QR codes
         *     param img grayscale or color (BGR) image containing QR codes.
         *     param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         *     param points optional output vector of vertices of the found QR code quadrangles. Will be empty if not found.
         * return automatically generated
         */
        public bool detectAndDecodeMulti(Mat img, List<string> decoded_info, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            bool retVal = objdetect_QRCodeDetector_detectAndDecodeMulti_11(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, points.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            return retVal;
        }

        /**
         * Both detects and decodes QR codes
         *     param img grayscale or color (BGR) image containing QR codes.
         *     param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
         * return automatically generated
         */
        public bool detectAndDecodeMulti(Mat img, List<string> decoded_info)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            bool retVal = objdetect_QRCodeDetector_detectAndDecodeMulti_12(nativeObj, img.nativeObj, decoded_info_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            return retVal;
        }


#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif



        // C++:   cv::QRCodeDetector::QRCodeDetector()
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_QRCodeDetector_10();

        // C++:  void cv::QRCodeDetector::setEpsX(double epsX)
        [DllImport(LIBNAME)]
        private static extern void objdetect_QRCodeDetector_setEpsX_10(IntPtr nativeObj, double epsX);

        // C++:  void cv::QRCodeDetector::setEpsY(double epsY)
        [DllImport(LIBNAME)]
        private static extern void objdetect_QRCodeDetector_setEpsY_10(IntPtr nativeObj, double epsY);

        // C++:  void cv::QRCodeDetector::setUseAlignmentMarkers(bool useAlignmentMarkers)
        [DllImport(LIBNAME)]
        private static extern void objdetect_QRCodeDetector_setUseAlignmentMarkers_10(IntPtr nativeObj, [MarshalAs(UnmanagedType.U1)] bool useAlignmentMarkers);

        // C++:  bool cv::QRCodeDetector::detect(Mat img, Mat& points)
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_detect_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);

        // C++:  string cv::QRCodeDetector::decode(Mat img, Mat points, Mat& straight_qrcode = Mat())
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_decode_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr straight_qrcode_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_decode_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);

        // C++:  String cv::QRCodeDetector::decodeCurved(Mat img, Mat points, Mat& straight_qrcode = Mat())
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_decodeCurved_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr straight_qrcode_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_decodeCurved_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);

        // C++:  string cv::QRCodeDetector::detectAndDecode(Mat img, Mat& points = Mat(), Mat& straight_qrcode = Mat())
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecode_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr straight_qrcode_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecode_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecode_12(IntPtr nativeObj, IntPtr img_nativeObj);

        // C++:  string cv::QRCodeDetector::detectAndDecodeCurved(Mat img, Mat& points = Mat(), Mat& straight_qrcode = Mat())
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecodeCurved_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr straight_qrcode_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecodeCurved_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_QRCodeDetector_detectAndDecodeCurved_12(IntPtr nativeObj, IntPtr img_nativeObj);

        // C++:  bool cv::QRCodeDetector::detectMulti(Mat img, Mat& points)
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_detectMulti_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj);

        // C++:  bool cv::QRCodeDetector::decodeMulti(Mat img, Mat points, vector_string& decoded_info, vector_Mat& straight_qrcode = vector_Mat())
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_decodeMulti_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr straight_qrcode_mat_nativeObj);
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_decodeMulti_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr decoded_info_mat_nativeObj);

        // C++:  bool cv::QRCodeDetector::detectAndDecodeMulti(Mat img, vector_string& decoded_info, Mat& points = Mat(), vector_Mat& straight_qrcode = vector_Mat())
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_detectAndDecodeMulti_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr points_nativeObj, IntPtr straight_qrcode_mat_nativeObj);
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_detectAndDecodeMulti_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr points_nativeObj);
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_QRCodeDetector_detectAndDecodeMulti_12(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj);

        // native support for java finalize()
        [DllImport(LIBNAME)]
        private static extern void objdetect_QRCodeDetector_delete(IntPtr nativeObj);

    }
}
