//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Wrapper to the Mat of the ZED
using System;
using System.Runtime.InteropServices;


namespace sl
{

    [StructLayout(LayoutKind.Sequential)]
    public struct char2
    {
        public byte r;
        public byte g;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct char3
    {
        public byte r;
        public byte g;
        public byte b;
    }
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
    [StructLayout(LayoutKind.Sequential)]
    public struct float2
    {
        public float r;
        public float g;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct float3
    {
        public float r;
        public float g;
        public float b;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct float4
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    public class ZEDMat
    {
        public enum MAT_TYPE
        {
            MAT_32F_C1, /*!< float 1 channel.*/
            MAT_32F_C2, /*!< float 2 channels.*/
            MAT_32F_C3, /*!< float 3 channels.*/
            MAT_32F_C4, /*!< float 4 channels.*/
            MAT_8U_C1, /*!< unsigned char 1 channel.*/
            MAT_8U_C2, /*!< unsigned char 2 channels.*/
            MAT_8U_C3, /*!< unsigned char 3 channels.*/
            MAT_8U_C4 /*!< unsigned char 4 channels.*/
        };

        public enum COPY_TYPE
        {
            COPY_TYPE_CPU_CPU, /*!< copy data from CPU to CPU.*/
            COPY_TYPE_CPU_GPU, /*!< copy data from CPU to GPU.*/
            COPY_TYPE_GPU_GPU, /*!< copy data from GPU to GPU.*/
            COPY_TYPE_GPU_CPU /*!< copy data from GPU to CPU.*/
        };

        public enum MEM
        {
            MEM_CPU = 1, /*!< CPU Memory (Processor side).*/
            MEM_GPU = 2 /*!< GPU Memory (Graphic card side).*/
        };
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

        private System.IntPtr _matInternalPtr;

        /// <summary>
        /// Returns the internal ptr of a mat
        /// </summary>
        public IntPtr MatPtr
        {
            get { return _matInternalPtr; }
        }

        /// <summary>
        /// Creates an empty mat
        /// </summary>
        public ZEDMat()
        {
            _matInternalPtr = dllz_mat_create_new_empty();
        }

        /// <summary>
        /// Creates a mat from
        /// </summary>
        /// <param name="ptr"></param>
        public ZEDMat(System.IntPtr ptr) 
        {
            if(ptr == IntPtr.Zero)
            {
                throw new Exception("ZED Mat not initialized");
            }
            _matInternalPtr = ptr;
        }

        /// <summary>
        /// Creates a Mat with a resolution
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="type"></param>
        /// <param name="mem"></param>
        public ZEDMat(sl.Resolution resolution, MAT_TYPE type, MEM mem = MEM.MEM_CPU)
        {
            _matInternalPtr = dllz_mat_create_new(resolution, (int)(type), (int)(mem));
        }

        /// <summary>
        /// Creates a Mat
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="type"></param>
        /// <param name="mem"></param>
        public ZEDMat(uint width, uint height, MAT_TYPE type, MEM mem = MEM.MEM_CPU)
        {
            _matInternalPtr = dllz_mat_create_new(new sl.Resolution(width, height), (int)(type), (int)(mem));
        }

        /// <summary>
        /// Defines whether the Mat is initialized or not.
        /// </summary>
        /// <returns></returns>
        public bool IsInit()
        {
            return dllz_mat_is_init(_matInternalPtr);
        }

        /// <summary>
        /// Free the memory of the Mat
        /// </summary>
        /// <param name="mem"></param>
        public void Free(MEM mem = (MEM.MEM_GPU | MEM.MEM_CPU))
        {
            dllz_mat_free(_matInternalPtr, (int)mem);
            _matInternalPtr = System.IntPtr.Zero;
        }

        /// <summary>
        /// Downloads data from DEVICE (GPU) to HOST (CPU), if possible.
        /// </summary>
        /// <returns></returns>
        public sl.ERROR_CODE UpdateCPUFromGPU()
        {
            return (sl.ERROR_CODE)dllz_mat_update_cpu_from_gpu(_matInternalPtr);
        }

        /// <summary>
        /// Uploads data from HOST (CPU) to DEVICE (GPU), if possible.
        /// </summary>
        /// <returns></returns>
        public sl.ERROR_CODE UpdateGPUFromCPU()
        {
            return (sl.ERROR_CODE)dllz_mat_update_gpu_from_cpu(_matInternalPtr);
        }

        /// <summary>
        /// Return the informations about the Mat
        /// </summary>
        /// <returns></returns>
        public string GetInfos()
        {
            byte[] buf = new byte[300];
            dllz_mat_get_infos(_matInternalPtr, buf);
            return System.Text.Encoding.ASCII.GetString(buf);
        }

        /// <summary>
        /// Copies data an other Mat (deep copy).
        /// </summary>
        /// <param name="dest">dst : the Mat where the data will be copied.</param>
        /// <param name="copyType">cpyType : specify the memories that will be used for the copy.</param>
        /// <returns></returns>
        public sl.ERROR_CODE CopyTo(sl.ZEDMat dest, sl.ZEDMat.COPY_TYPE copyType = COPY_TYPE.COPY_TYPE_CPU_CPU)
        {
            return (sl.ERROR_CODE)dllz_mat_copy_to(_matInternalPtr, dest._matInternalPtr, (int)(copyType));
        }

        /// <summary>
        /// Reads an image from a file (only if \ref MEM_CPU is available on the current
        /// </summary>
        /// <param name="filePath"> file path including the name and extension.</param>
        /// <returns></returns>
        public sl.ERROR_CODE Read(string filePath)
        {
            return (sl.ERROR_CODE)dllz_mat_read(_matInternalPtr, filePath);
        }

        /// <summary>
        ///  Writes the Mat (only if MEM_CPU is available) into a file as an image.
        /// </summary>
        /// <param name="filePath">file path including the name and extension.</param>
        /// <returns></returns>
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
        /// Returns the number of values stored in one pixel.
        /// </summary>
        /// <returns></returns>
        public int GetChannels()
        {
            return dllz_mat_get_channels(_matInternalPtr);
        }

        /// <summary>
        /// Returns the size in bytes of one pixel.
        /// </summary>
        /// <returns></returns>
        public int GetPixelBytes()
        {
            return dllz_mat_get_pixel_bytes(_matInternalPtr);
        }

        /// <summary>
        ///  Returns the memory step in number of elements (the number of values in one pixel row).
        /// </summary>
        /// <returns></returns>
        public int GetStep()
        {
            return dllz_mat_get_step(_matInternalPtr);
        }

        /// <summary>
        /// Returns the memory step in Bytes (the Bytes size of one pixel row).
        /// </summary>
        /// <returns></returns>
        public int GetStepBytes()
        {
            return dllz_mat_get_step_bytes(_matInternalPtr);
        }

        /// <summary>
        /// Returns the size in bytes of a row.
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
        /// Returns whether the Mat is the owner of the memory it access.
        /// </summary>
        /// <returns></returns>
        public bool IsMemoryOwner()
        {
            return dllz_mat_is_memory_owner(_matInternalPtr);
        }

        public sl.Resolution GetResolution()
        {
            return dllz_mat_get_resolution(_matInternalPtr);
        }

        /// <summary>
        /// Allocates the Mat memory.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="matType"></param>
        /// <param name="mem"></param>
        public void Alloc(uint width, uint height, MAT_TYPE matType, MEM mem = MEM.MEM_CPU)
        {
            dllz_mat_alloc(_matInternalPtr, (int)width, (int)height, (int)matType, (int)mem);
        }

        /// <summary>
        /// Allocates the Mat memory.
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="matType"></param>
        /// <param name="mem"></param>
        public void Alloc(sl.Resolution resolution, MAT_TYPE matType, MEM mem = MEM.MEM_CPU)
        {
            dllz_mat_alloc(_matInternalPtr, (int)resolution.width, (int)resolution.height, (int)matType, (int)mem);
        }

        /// <summary>
        /// Copies data from an other Mat (deep copy).
        /// </summary>
        /// <param name="src"></param>
        /// <param name="copyType"></param>
        /// <returns></returns>
        public int SetFrom(ZEDMat src, COPY_TYPE copyType = COPY_TYPE.COPY_TYPE_CPU_CPU)
        {
            return dllz_mat_set_from(_matInternalPtr, src._matInternalPtr, (int)copyType);
        }

        public System.IntPtr GetPtr(MEM mem = MEM.MEM_CPU)
        {
            return dllz_mat_get_ptr(_matInternalPtr, (int)mem);
        }

        /// <summary>
        /// Duplicates Mat by copy (deep copy).
        /// </summary>
        /// <param name="source"></param>
        public void Clone(ZEDMat source)
        {
            dllz_mat_clone(_matInternalPtr, source._matInternalPtr);
        }

        /************ GET VALUES *********************/
        // Cannot send values by template, covariant issue with a out needed

        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float2(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float3(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_float4(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar2(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar3(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /// <summary>
        /// Returns the value of a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE GetValue(int x, int y, out char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_get_value_uchar4(_matInternalPtr, x, y, out value, (int)(mem)));
        }
        /***************************************************************************************/


        /************ SET VALUES *********************/
        // Cannot send values by template, covariant issue with a out needed

        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float2(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float3(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_float4(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar2(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar3(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /// <summary>
        /// Sets a value to a specific point in the matrix.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetValue(int x, int y, ref char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_value_uchar4(_matInternalPtr, x, y, ref value, (int)(mem)));
        }
        /***************************************************************************************/

        /************ SET TO *********************/
        // Cannot send values by template, covariant issue with a out needed

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref float value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float(_matInternalPtr, ref value, (int)(mem)));
        }


        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref float2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float2(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref float3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float3(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref float4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_float4(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref byte value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref char2 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar2(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo(ref char3 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar3(_matInternalPtr, ref value, (int)(mem)));
        }

        /// <summary>
        /// ills the Mat with the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        public sl.ERROR_CODE SetTo( ref char4 value, sl.ZEDMat.MEM mem)
        {
            return (sl.ERROR_CODE)(dllz_mat_set_to_uchar4(_matInternalPtr, ref value, (int)(mem)));
        }
        /***************************************************************************************/

    }
}
