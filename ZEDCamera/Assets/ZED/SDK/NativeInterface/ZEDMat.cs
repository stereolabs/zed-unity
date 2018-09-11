//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using System;
using System.Runtime.InteropServices;

/// <summary>
/// This file holds the ZEDMat class along with low-level structures used for passing data between 
/// the C# ZEDMat and its equivalent in the SDK. 
/// </summary>

namespace sl
{
    /// <summary>
    /// Represents a 2D vector of uchars for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct char2
    {
        public byte r;
        public byte g;
    }

    /// <summary>
    /// Represents a 3D vector of uchars for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct char3
    {
        public byte r;
        public byte g;
        public byte b;
    }

    /// <summary>
    /// Represents a 4D vector of uchars for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct char4
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte r;
        [MarshalAs(UnmanagedType.U1)]
        public byte g;
        [MarshalAs(UnmanagedType.U1)]
        public byte b;
        [MarshalAs(UnmanagedType.U1)]
        public byte a;
    }

    /// <summary>
    /// Represents a 2D vector of floats for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct float2
    {
        public float r;
        public float g;
    }
    /// <summary>
    /// Represents a 3D vector of floats for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct float3
    {
        public float r;
        public float g;
        public float b;
    }
    /// <summary>
    /// Represents a 4D vector of floats for use on both the CPU and GPU. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct float4
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    /// <summary>
    /// Mirrors the sl::Mat class used in the ZED C++ SDK to store images. 
    /// Can be used to retrieve individual images from GPU or CPU memory: see ZEDCamera.RetrieveImage() 
    /// and ZEDCamera.RetrieveMeasure(). 
    /// </summary><remarks>
    /// For more information on the Mat class it mirrors, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/classsl_1_1Mat.html
    /// </remarks>
    public class ZEDMat
    {
        /// <summary>
        /// Type of mat, indicating the data type and the number of channels it holds. 
        /// Proper mat type depends on the image type. See sl.VIEW and sl.MEASURE (in ZEDCommon.cs)
        /// </summary>
        public enum MAT_TYPE
        {
            /// <summary>
            /// Float, one channel. Used for depth and disparity Measure-type textures.
            /// </summary>
            MAT_32F_C1,
            /// <summary>
            /// Float, two channels. 
            /// </summary>
            MAT_32F_C2, 
            /// <summary>
            /// Float, three channels.
            /// </summary>
            MAT_32F_C3, /*!< float 3 channels.*/
            /// <summary>
            /// Float, four channels. Used for normals and XYZ (point cloud) measure-type textures 
            /// </summary>
            MAT_32F_C4, 
            /// <summary>
            /// Unsigned char, one channel. Used for greyscale image-type textures like depth and confidence displays. 
            /// </summary>
            MAT_8U_C1,
            /// <summary>
            /// Unsigned char, two channels. 
            /// </summary>
            MAT_8U_C2,
            /// <summary>
            /// Unsigned char, three channels. 
            /// </summary>
            MAT_8U_C3, 
            /// <summary>
            /// Unsigned char, four channels. Used for color images, like the main RGB image from each sensor. 
            /// </summary>
            MAT_8U_C4 
        };

        /// <summary>
        /// Categories for copying data within or between the CPU (processor) memory and GPU (graphics card) memory.
        /// </summary>
        public enum COPY_TYPE
        {
            /// <summary>
            /// Copies data from one place in CPU memory to another. 
            /// </summary>
            COPY_TYPE_CPU_CPU, /*!< copy data from CPU to CPU.*/
            /// <summary>
            /// Copies data from CPU memory to GPU memory.
            /// </summary>
            COPY_TYPE_CPU_GPU, /*!< copy data from CPU to GPU.*/
            /// <summary>
            /// Copies data from one place in GPU memory to another. 
            /// </summary>
            COPY_TYPE_GPU_GPU, /*!< copy data from GPU to GPU.*/
            /// <summary>
            /// Copies data from GPU memory to CPU memory. 
            /// </summary>
            COPY_TYPE_GPU_CPU /*!< copy data from GPU to CPU.*/
        };

        /// <summary>
        /// Which memory to store an image/mat: CPU/processor memory or GPU (graphics card) memory.
        /// </summary>
        public enum MEM
        {
            /// <summary>
            /// Store on memory accessible by the CPU. 
            /// </summary>
            MEM_CPU = 1,
            /// <summary>
            /// Store on memory accessible by the GPU. 
            /// </summary>
            MEM_GPU = 2 
        };

        #region DLL Calls
        const string nameDll = "sl_unitywrapper";

        [DllImport(nameDll, EntryPoint = "dllz_mat_create_new")]
        private static extern IntPtr dllz_mat_create_new(sl.Resolution resolution, int type, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_create_new_empty")]
        private static extern IntPtr dllz_mat_create_new_empty();


        [DllImport(nameDll, EntryPoint = "dllz_mat_is_init")]
        private static extern bool dllz_mat_is_init(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_free")]
        private static extern bool dllz_mat_free(System.IntPtr ptr, int type);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_infos")]
        private static extern bool dllz_mat_get_infos(System.IntPtr ptr, byte[] buffer);


        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_float")]
        private static extern int dllz_mat_get_value_float(System.IntPtr ptr, int x, int y, out float value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_float2")]
        private static extern int dllz_mat_get_value_float2(System.IntPtr ptr, int x, int y, out float2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_float3")]
        private static extern int dllz_mat_get_value_float3(System.IntPtr ptr, int x, int y, out float3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_float4")]
        private static extern int dllz_mat_get_value_float4(System.IntPtr ptr, int x, int y, out float4 value, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_uchar")]
        private static extern int dllz_mat_get_value_uchar(System.IntPtr ptr, int x, int y, out byte value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_uchar2")]
        private static extern int dllz_mat_get_value_uchar2(System.IntPtr ptr, int x, int y, out char2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_uchar3")]
        private static extern int dllz_mat_get_value_uchar3(System.IntPtr ptr, int x, int y, out char3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_get_value_uchar4")]
        private static extern int dllz_mat_get_value_uchar4(System.IntPtr ptr, int x, int y, out char4 value, int mem);


        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_float")]
        private static extern int dllz_mat_set_value_float(System.IntPtr ptr, int x, int y, ref float value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_float2")]
        private static extern int dllz_mat_set_value_float2(System.IntPtr ptr, int x, int y, ref float2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_float3")]
        private static extern int dllz_mat_set_value_float3(System.IntPtr ptr, int x, int y, ref float3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_float4")]
        private static extern int dllz_mat_set_value_float4(System.IntPtr ptr, int x, int y, ref float4 value, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_uchar")]
        private static extern int dllz_mat_set_value_uchar(System.IntPtr ptr, int x, int y, ref byte value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_uchar2")]
        private static extern int dllz_mat_set_value_uchar2(System.IntPtr ptr, int x, int y, ref char2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_uchar3")]
        private static extern int dllz_mat_set_value_uchar3(System.IntPtr ptr, int x, int y, ref char3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_value_uchar4")]
        private static extern int dllz_mat_set_value_uchar4(System.IntPtr ptr, int x, int y, ref char4 value, int mem);


        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_float")]
        private static extern int dllz_mat_set_to_float(System.IntPtr ptr, ref float value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_float2")]
        private static extern int dllz_mat_set_to_float2(System.IntPtr ptr, ref float2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_float3")]
        private static extern int dllz_mat_set_to_float3(System.IntPtr ptr, ref float3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_float4")]
        private static extern int dllz_mat_set_to_float4(System.IntPtr ptr, ref float4 value, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_uchar")]
        private static extern int dllz_mat_set_to_uchar(System.IntPtr ptr,  ref byte value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_uchar2")]          
        private static extern int dllz_mat_set_to_uchar2(System.IntPtr ptr, ref char2 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_uchar3")]          
        private static extern int dllz_mat_set_to_uchar3(System.IntPtr ptr, ref char3 value, int mem);
        [DllImport(nameDll, EntryPoint = "dllz_mat_set_to_uchar4")]
        private static extern int dllz_mat_set_to_uchar4(System.IntPtr ptr, ref char4 value, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_update_cpu_from_gpu")]
        private static extern int dllz_mat_update_cpu_from_gpu(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_update_gpu_from_cpu")]
        private static extern int dllz_mat_update_gpu_from_cpu(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_read")]
        private static extern int dllz_mat_read(System.IntPtr ptr, string filePath);

        [DllImport(nameDll, EntryPoint = "dllz_mat_write")]
        private static extern int dllz_mat_write(System.IntPtr ptr, string filePath);

        [DllImport(nameDll, EntryPoint = "dllz_mat_copy_to")]
        private static extern int dllz_mat_copy_to(System.IntPtr ptr, System.IntPtr dest, int cpyType);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_width")]
        private static extern int dllz_mat_get_width(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_height")]
        private static extern int dllz_mat_get_height(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_channels")]
        private static extern int dllz_mat_get_channels(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_memory_type")]
        private static extern int dllz_mat_get_memory_type(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_pixel_bytes")]
        private static extern int dllz_mat_get_pixel_bytes(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_step")]
        private static extern int dllz_mat_get_step(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_step_bytes")]
        private static extern int dllz_mat_get_step_bytes(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_width_bytes")]
        private static extern int dllz_mat_get_width_bytes(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_is_memory_owner")]
        private static extern bool dllz_mat_is_memory_owner(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_resolution")]
        private static extern sl.Resolution dllz_mat_get_resolution(System.IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "dllz_mat_alloc")]
        private static extern void dllz_mat_alloc(System.IntPtr ptr, int width, int height, int type, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_set_from")]
        private static extern int dllz_mat_set_from(System.IntPtr ptr, System.IntPtr source, int copyType);

        [DllImport(nameDll, EntryPoint = "dllz_mat_get_ptr")]
        private static extern System.IntPtr dllz_mat_get_ptr(System.IntPtr ptr, int mem);

        [DllImport(nameDll, EntryPoint = "dllz_mat_clone")]
        private static extern void dllz_mat_clone(System.IntPtr ptr, System.IntPtr ptrSource);

        #endregion

        /// <summary>
        /// Returns the internal ptr of a Mat. 
        /// </summary>
        private System.IntPtr _matInternalPtr;
        /// <summary>
        /// Returns the internal ptr of a Mat.
        /// </summary>
        public IntPtr MatPtr
        {
            get { return _matInternalPtr; }
        }

        /// <summary>
        /// Creates an empty Mat.
        /// </summary>
        public ZEDMat()
        {
            _matInternalPtr = dllz_mat_create_new_empty();
        }

        /// <summary>
        /// Creates a mat from an existing internal ptr.
        /// </summary>
        /// <param name="ptr">IntPtr to create the material with.</param>
        public ZEDMat(System.IntPtr ptr) 
        {
            if(ptr == IntPtr.Zero)
            {
                throw new Exception("ZED Mat not initialized.");
            }
            _matInternalPtr = ptr;
        }

        /// <summary>
        /// Creates a Mat with a given resolution.
        /// </summary>
        /// <param name="resolution">Resolution for the new Mat.</param>
        /// <param name="type">Data type and number of channels the Mat will hold.
        /// Depends on texture type: see sl.VIEW and sl.MEASURE in ZEDCommon.cs.</param>
        /// <param name="mem">Whether Mat should exist on CPU or GPU memory.
        /// Choose depending on where you'll need to access it from.</param>
        public ZEDMat(sl.Resolution resolution, MAT_TYPE type, MEM mem = MEM.MEM_CPU)
        {
            _matInternalPtr = dllz_mat_create_new(resolution, (int)(type), (int)(mem));
        }

        /// <summary>
        /// Creates a Mat with a given width and height.
        /// </summary>
        /// <param name="width">Width of the new Mat.</param>
        /// <param name="height">Height of the new Mat.</param>
        /// <param name="type">Data type and number of channels the Mat will hold.
        /// Depends on texture type: see sl.VIEW and sl.MEASURE in ZEDCommon.cs.</param>
        /// <param name="mem">Whether Mat should exist on CPU or GPU memory.
        /// Choose depending on where you'll need to access it from.</param>
        public ZEDMat(uint width, uint height, MAT_TYPE type, MEM mem = MEM.MEM_CPU)
        {
            _matInternalPtr = dllz_mat_create_new(new sl.Resolution(width, height), (int)(type), (int)(mem));
        }

        /// <summary>
        /// True if the Mat has been initialized.
        /// </summary>
        /// <returns></returns>
        public bool IsInit()
        {
            return dllz_mat_is_init(_matInternalPtr);
        }

        /// <summary>
        /// Frees the memory of the Mat.
        /// </summary>
        /// <param name="mem">Whether the Mat is on CPU or GPU memory.</param>
        public void Free(MEM mem = (MEM.MEM_GPU | MEM.MEM_CPU))
        {
            dllz_mat_free(_matInternalPtr, (int)mem);
            _matInternalPtr = System.IntPtr.Zero;
        }

        /// <summary>
        /// Copies data from the GPU to the CPU, if possible.
        /// </summary>
        /// <returns></returns>
        public sl.ERROR_CODE UpdateCPUFromGPU()
        {
            return (sl.ERROR_CODE)dllz_mat_update_cpu_from_gpu(_matInternalPtr);
        }

        /// <summary>
        /// Copies data from the CPU to the GPU, if possible.
        /// </summary>
        /// <returns></returns>
        public sl.ERROR_CODE UpdateGPUFromCPU()
        {
            return (sl.ERROR_CODE)dllz_mat_update_gpu_from_cpu(_matInternalPtr);
        }

        /// <summary>
        /// Returns information about the Mat.
        /// </summary>
        /// <returns>String providing Mat information.</returns>
        public string GetInfos()
        {
            byte[] buf = new byte[300];
            dllz_mat_get_infos(_matInternalPtr, buf);
            return System.Text.Encoding.ASCII.GetString(buf);
        }

        /// <summary>
        /// Copies data from this Mat to another Mat (deep copy).
        /// </summary>
        /// <param name="dest">Mat that the data will be copied to.</param>
        /// <param name="copyType">The To and From memory types.</param>
        /// <returns>Error code indicating if the copy was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE CopyTo(sl.ZEDMat dest, sl.ZEDMat.COPY_TYPE copyType = COPY_TYPE.COPY_TYPE_CPU_CPU)
        {
            return (sl.ERROR_CODE)dllz_mat_copy_to(_matInternalPtr, dest._matInternalPtr, (int)(copyType));
        }
        
        /// <summary>
        /// Reads an image from a file. Supports .png and .jpeg. Only works if Mat has access to MEM_CPU.
        /// </summary>
        /// <param name="filePath">File path, including file name and extension.</param>
        /// <returns>Error code indicating if the read was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE Read(string filePath)
        {
            return (sl.ERROR_CODE)dllz_mat_read(_matInternalPtr, filePath);
        }

        /// <summary>
        /// Writes the Mat into a file as an image. Only works if Mat has access to MEM_CPU.
        /// </summary>
        /// <param name="filePath">File path, including file name and extension.</param>
        /// <returns>Error code indicating if the write was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE Write(string filePath)
        {
            return (sl.ERROR_CODE)dllz_mat_write(_matInternalPtr, filePath);
        }

        /// <summary>
        /// Returns the width of the matrix.
        /// </summary>
        /// <returns></returns>
        public int GetWidth()
        {
            return dllz_mat_get_width(_matInternalPtr);
        }

        /// <summary>
        /// Returns the height of the matrix.
        /// </summary>
        /// <returns></returns>
        public int GetHeight()
        {
            return dllz_mat_get_height(_matInternalPtr);
        }

        /// <summary>
        /// Returns the number of values/channels stored in each pixel.
        /// </summary>
        /// <returns>Number of values/channels.</returns>
        public int GetChannels()
        {
            return dllz_mat_get_channels(_matInternalPtr);
        }

        /// <summary>
        /// Returns the size in bytes of one pixel.
        /// </summary>
        /// <returns>Size in bytes.</returns>
        public int GetPixelBytes()
        {
            return dllz_mat_get_pixel_bytes(_matInternalPtr);
        }

        /// <summary>
        ///  Returns the memory 'step' in number/length of elements - how many values make up each row of pixels.
        /// </summary>
        /// <returns>Step length.</returns>
        public int GetStep()
        {
            return dllz_mat_get_step(_matInternalPtr);
        }

        /// <summary>
        /// Returns the memory 'step' in bytes - how many bytes make up each row of pixels.
        /// </summary>
        /// <returns></returns>
        public int GetStepBytes()
        {
            return dllz_mat_get_step_bytes(_matInternalPtr);
        }

        /// <summary>
        /// Returns the size of each row in bytes.
        /// </summary>
        /// <returns></returns>
        public int GetWidthBytes()
        {
            return dllz_mat_get_width_bytes(_matInternalPtr);
        }

        /// <summary>
        /// Returns the type of memory (CPU and/or GPU).
        /// </summary>
        /// <returns></returns>
        public MEM GetMemoryType()
        {
            return (MEM)dllz_mat_get_memory_type(_matInternalPtr);
        }

        /// <summary>
        /// Returns whether the Mat is the owner of the memory it's accessing.
        /// </summary>
        /// <returns></returns>
        public bool IsMemoryOwner()
        {
            return dllz_mat_is_memory_owner(_matInternalPtr);
        }

        /// <summary>
        /// Returns the resolution of the image that this Mat holds. 
        /// </summary>
        /// <returns></returns>
        public sl.Resolution GetResolution()
        {
            return dllz_mat_get_resolution(_matInternalPtr);
        }

        /// <summary>
        /// Allocates memory for the Mat.
        /// </summary>
        /// <param name="width">Width of the image/matrix in pixels.</param>
        /// <param name="height">Height of the image/matrix in pixels.</param>
        /// <param name="matType">Type of matrix (data type and channels; see sl.MAT_TYPE)</param>
        /// <param name="mem">Where the buffer will be stored - CPU memory or GPU memory.</param>
        public void Alloc(uint width, uint height, MAT_TYPE matType, MEM mem = MEM.MEM_CPU)
        {
            dllz_mat_alloc(_matInternalPtr, (int)width, (int)height, (int)matType, (int)mem);
        }

        /// <summary>
        /// Allocates memory for the Mat.
        /// </summary>
        /// <param name="resolution">Size of the image/matrix in pixels.</param>
        /// <param name="matType">Type of matrix (data type and channels; see sl.MAT_TYPE)</param>
        /// <param name="mem">Where the buffer will be stored - CPU memory or GPU memory.</param>
        public void Alloc(sl.Resolution resolution, MAT_TYPE matType, MEM mem = MEM.MEM_CPU)
        {
            dllz_mat_alloc(_matInternalPtr, (int)resolution.width, (int)resolution.height, (int)matType, (int)mem);
        }

        /// <summary>
        /// Copies data from another Mat into this one(deep copy).
        /// </summary>
        /// <param name="src">Source Mat from which to copy.</param>
        /// <param name="copyType">The To and From memory types.</param>
        /// <returns>ERROR_CODE (as an int) indicating if the copy was successful, or why it wasn't.</returns>
        public int SetFrom(ZEDMat src, COPY_TYPE copyType = COPY_TYPE.COPY_TYPE_CPU_CPU)
        {
            return dllz_mat_set_from(_matInternalPtr, src._matInternalPtr, (int)copyType);
        }

        public System.IntPtr GetPtr(MEM mem = MEM.MEM_CPU)
        {
            return dllz_mat_get_ptr(_matInternalPtr, (int)mem);
        }

        /// <summary>
        /// Duplicates a Mat by copying all its data into a new one (deep copy).
        /// </summary>
        /// <param name="source"></param>
        public void Clone(ZEDMat source)
        {
            dllz_mat_clone(_matInternalPtr, source._matInternalPtr);
        }

        /************ GET VALUES *********************/
        //Cannot send values by template due to a covariant issue with an out needed.

        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_32F_C1)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_32F_C2)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float2(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_32F_C3)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float3(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_32F_C4)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float4(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_TYPE_8U_C1)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_TYPE_8U_C2)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar2(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_TYPE_8U_C3)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar3(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix. (MAT_TYPE_8U_C4)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Gets filled with the current value.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the get was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar4(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /***************************************************************************************/


        /************ SET VALUES *********************/
        //Cannot send values by template due to a covariant issue with an out needed.

        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_32F_C1)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_32F_C2)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float2(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_32F_C3)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float3(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_32F_C4)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float4(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_TYPE_8U_C1)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_TYPE_8U_C2)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar2(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_TYPE_8U_C3)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar3(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix. (MAT_TYPE_8U_C4)
        /// </summary>
        /// <param name="x">Row the point is in.</param>
        /// <param name="y">Column the point is in.</param>
        /// <param name="value">Value to which the point will be set.</param>
        /// <param name="mem">Whether point is on CPU memory or GPU memory.</param>
        /// <returns>Error code indicating if the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar4(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /***************************************************************************************/

        /************ SET TO *********************/
        //Cannot send values by template due to a covariant issue with an out needed.

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_32F_C1)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_32F_C2)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float2(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_32F_C3)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float3(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_32F_C4)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float4(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_TYPE_8U_C1)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_TYPE_8U_C2)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar2(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_TYPE_8U_C3)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo(ref char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar3(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// Fills the entire Mat with the given value. (MAT_TYPE_8U_C4)
        /// </summary>
        /// <param name="value">Value with which to fill the Mat.</param>
        /// <param name="mem">Which buffer to fill - CPU or GPU memory.</param>
        /// <returns>Whether the set was successful, or why it wasn't.</returns>
        public sl.ERROR_CODE SetTo( ref char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar4(_matInternalPtr, ref value, (int)(mem)));
        }
        /***************************************************************************************/

    }
}
