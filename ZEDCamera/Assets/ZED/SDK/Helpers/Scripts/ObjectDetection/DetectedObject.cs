using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sl;
using System;

/// <summary>
/// Represents a single object detected by the ZED Object Detection module. 
/// Provides various functions for knowing where the object is in the world (position) and how much space it takes up (bounds).
/// <para>It's listed in a DetectionFrame, which is included as the argument in the ZEDManager.OnObjectDetection event.</para>
/// </summary><remarks>
/// This is a higher level version of sl.ObjectData, which comes directly from the ZED SDK and doesn't follow Unity conventions. 
/// </remarks>
public class DetectedObject
{
    private ObjectDataSDK objectData;
    /// <summary>
    /// The raw ObjectData object that this instance is an abstraction of - ObjectData's data comes
    /// directly from the SDK and doesn't follow Unity conventions. 
    /// </summary>
    public ObjectDataSDK rawObjectData
    {
        get
        {
            return objectData;
        }
    }

    /// <summary>
    /// Arbitrary ID assigned to the object on first detection. Will persist between frames if the object remains
    /// visible and detected. 
    /// </summary>
    public int id
    {
        get
        {
            return objectData.id;
        }
    }

    /// <summary>
    /// Class of object that was detected (person, vehicle, etc.)
    /// </summary>
    public OBJECT_CLASS objectClass
    {
        get
        {
            return objectData.objectClass;
        }
    }

    /// <summary>
    /// SubClass of object that was detected  
    /// </summary>
    public OBJECT_SUBCLASS objectSubClass
    {
        get
        {
            return objectData.objectSubClass;
        }
    }

    /// <summary>
    /// Current state of the object's tracking. "OK" means it's visible this frame. "Searching" means it was
    /// visible recently, but is no longer visible. "OFF" means that object detection has tracking turned off, 
    /// so 3D data will never be available. 
    /// </summary>
    public OBJECT_TRACK_STATE trackingState
    {
        get
        {
            return objectData.objectTrackingState;
        }
    }

    /// <summary>
    /// Current action state. "IDLE" means the object is not moving. "MOVING" means the object is moving.
    /// </summary>
    public OBJECT_ACTION_STATE actionState
    {
        get
        {
            return objectData.actionState;
        }
    }


    /// <summary>
    /// How confident the ZED SDK is that this object is in fact a valid detection. From 1 to 100. 
    /// Higher is better, eg. if objectClass is PERSON and confidence is 99, there's a 99% chance it's indeed a person. 
    /// <para>You can set the minimum confidence threshold in ZEDManager's Inspector.</para>
    /// </summary>
    public float confidence
    {
        get
        {
            return objectData.confidence;
        }
    }

    /// <summary>
    /// The manager class responsible for the ZED camera that detected this object. 
    /// </summary>
    public ZEDManager detectingZEDManager;

    private Vector3 camPositionAtDetection;
    private Quaternion camRotationAtDetection;

    /// <summary>
    /// sl::Mat material that represents where on the image the detected object exists on a pixel-by-pixel level. 
    /// For a texture that you can overlay, use GetMaskTexture(). 
    /// </summary>
    public sl.ZEDMat maskMat;

    /// <summary>
    /// Cached texture version of the 2D mask. Retrieved with GetMaskTexture. 
    /// This is cached after calculating the first time to avoid needless calculation from multiple calls.
    /// </summary>
    private Texture2D maskTexture = null;

    /// <summary>
    /// Cached texture version of the 2D mask. Retrieved with GetMaskTexture. 
    /// Flipped on the Y axis to make it simpler to apply to standard Unity shaders. 
    /// This is cached after calculating the first time to avoid needless calculation from multiple calls.
    /// </summary>
    private Texture2D maskTextureFlipped = null;

    /// <summary>
    /// Constructor that assigns values required for transformations later on. 
    /// This is necessary because detections are frozen in a particular frame, and results should not change
    /// in subsequent frames when the camera moves. 
    /// </summary>
    /// <param name="odata">Raw sl.ObjectData data that this instance represents.</param>
    /// <param name="viewingmanager">ZEDManager assigned to the ZED camera that detected the object.</param>
    /// <param name="campos">World position of the left ZED camera when the object was detected.</param>
    /// <param name="camrot">World rotation of the left ZED camera when the object was detected.</param>
    public DetectedObject(ObjectDataSDK odata, ZEDManager viewingmanager, Vector3 campos, Quaternion camrot)
    {
        objectData = odata;
        detectingZEDManager = viewingmanager;
        camPositionAtDetection = campos;
        camRotationAtDetection = camrot;

        //maskMat = new ZEDMat(odata.mask);
        //maskTexture = ZEDMatToTexture_CPU(maskMat);

    }

    /// <summary>
    /// Returns the pixel positions of the four corners of the object's 2D bounding box on the image. 
    /// <para>Like most of Unity, the Y values are relative to the bottom of the image, which is unlike the 
    /// raw imageBoundingBox data from the ObjectData struct.</para>
    ///  0 ------- 1
    ///  |   obj   |
    ///  3-------- 2
    /// </summary>
    /// <param name="scaleForCanvasUnityError">Adds an optional scaling factor to handle a bug in 2018.3 and greater where the
    /// canvas set to Screen Space - Camera has very weird offsets when its projection matrix has certain small changes made to it directly.</param>
    public Vector2[] Get2DBoundingBoxCorners_Image(float scaleForCanvasUnityError = 1)
    {
        //Shorthand. 
        float zedimagewidth = detectingZEDManager.zedCamera.ImageWidth;
        float zedimageheight = detectingZEDManager.zedCamera.ImageHeight;

        //Canvas offsets from horizontal and vertical calibration offsets (cx/cy). 
        CalibrationParameters calib = detectingZEDManager.zedCamera.GetCalibrationParameters();
        float cxoffset = zedimagewidth * scaleForCanvasUnityError / 2f - calib.leftCam.cx * scaleForCanvasUnityError;
        float cyoffset = 0 - (zedimageheight / 2f - calib.leftCam.cy);


        Vector2[] flippedYimagecoords = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector2 rawcoord;
            rawcoord.x = objectData.imageBoundingBox[i].x * scaleForCanvasUnityError + cxoffset;
            rawcoord.y = detectingZEDManager.zedCamera.ImageHeight - objectData.imageBoundingBox[i].y + cyoffset;
            

#if UNITY_2018_1_OR_NEWER
            //Terrible hack to compensate for bug in Unity that scales the Canvas very improperly if you have certain (necessary) values on the projection matrix. 
            rawcoord.y = (rawcoord.y - (zedimageheight / 2f)) * scaleForCanvasUnityError + (zedimageheight / 2f);
#endif

            flippedYimagecoords[i] = rawcoord;
        }

        return flippedYimagecoords;
    }

    /// <summary>
    /// Returns the viewport positions of the four corners of the object's 2D bounding box on the capturing camera. 
    /// <para>Like most of Unity, the Y values are relative to the bottom of the image, which is unlike the 
    /// raw imageBoundingBox data from the ObjectData struct.</para>
    ///  0 ------- 1
    ///  |   obj   |
    ///  3-------- 2
    /// </summary>
    /// <param name="scaleForCanvasUnityError">Adds an optional scaling factor to handle a bug in 2018.3 and greater where the
    /// canvas set to Screen Space - Camera has very weird offsets when its projection matrix has certain small changes made to it directly.</param>
    public Vector2[] Get2DBoundingBoxCorners_Viewport(float scaleForCanvasUnityError = 1)
    {
        Vector2[] imagecoords = Get2DBoundingBoxCorners_Image(scaleForCanvasUnityError);

        Vector2[] viewportcorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            viewportcorners[i] = detectingZEDManager.GetLeftCamera().ScreenToViewportPoint(imagecoords[i]);
        }

        return viewportcorners;
    }

    /// <summary>
    /// Returns the object's 2D bounding box as a rect. Use this to position a UI element around the object
    /// in a Camera-space UI canvas attached to the capturing camera. 
    /// Values assume Y is relative to the bottom. 
    /// </summary>
    /// <param name="scaleForCanvasUnityError">Adds an optional scaling factor to handle a bug in 2018.3 and greater where the
    /// canvas set to Screen Space - Camera has very weird offsets when its projection matrix has certain small changes made to it directly.</param>
    public Rect Get2DBoundingBoxRect(float scaleForCanvasUnityError = 1f)
    {
        Vector2[] imagecoords = Get2DBoundingBoxCorners_Image(scaleForCanvasUnityError);

        float width = (imagecoords[1].x - imagecoords[0].x);// * scaleForCanvas;
        float height = (imagecoords[0].y - imagecoords[3].y);

        Vector2 bottomleftcorner = imagecoords[3];

        return new Rect(bottomleftcorner, new Vector2(width, height));
    }

    /// <summary>
    /// Gets the center of the 3D object in world space. 
    /// </summary>
    public Vector3 Get3DWorldPosition()
    {
        //Get the center of the transformed bounding box. 
        float ypos = (localToWorld(objectData.worldBoundingBox[0]).y - localToWorld(objectData.worldBoundingBox[4]).y) / 2f + localToWorld(objectData.worldBoundingBox[4]).y;
        Vector3 transformedroot = localToWorld(objectData.rootWorldPosition);

        return new Vector3(transformedroot.x, ypos, transformedroot.z);
    }

    /// <summary>
    /// Gets the direction that the 3D bounding box is facing, relative to the world. 
    /// This is simply the opposite of the direction the camera was facing on detection. 
    /// </summary>
    /// <param name="boxesfacecamera">True to artificially rotate the boxes to face the camera.</param>
    public Quaternion Get3DWorldRotation(bool boxesfacecamera)
    {
        Vector3 normal;
        if (boxesfacecamera)
        {
            normal = Get3DWorldPosition() - detectingZEDManager.GetLeftCameraTransform().position; //This makes box face camera. 
        }
        else
        {
            normal = camRotationAtDetection * Vector3.forward; //This is to face the inverse of the camera's Z direction. 
        }

        normal.y = 0;
        return Quaternion.LookRotation(normal, Vector3.up);
    }

    /// <summary>
    /// Gets the size of the bounding box as a Bounds class. 
    /// Use this to draw lines around an object's bounds, or scale a cube to enclose it. 
    /// </summary>
    public Bounds Get3DWorldBounds()
    {
        Vector3[] worldcorners = objectData.worldBoundingBox;

        Quaternion pitchrot = GetRotationWithoutYaw();

        Vector3 leftbottomback = pitchrot * worldcorners[5]; //Shorthand.
        Vector3 righttopfront = pitchrot * worldcorners[3]; //Shorthand.

        float xsize = Mathf.Abs(righttopfront.x - leftbottomback.x);
        float ysize = Mathf.Abs(righttopfront.y - leftbottomback.y);
        float zsize = Mathf.Abs(righttopfront.z - leftbottomback.z);

        return new Bounds(Vector3.zero, new Vector3(xsize, ysize, zsize));
    }

    /// <summary>
    /// Gets the world positions of the eight corners of the object's 3D bounding box. 
    /// If facingCamera is set to true, the box will face the ZED that observed it. 
    /// If false, they will be aligned with the world axes. However, this can result in too large of a box
    /// since it has to encompass the camera-aligned version. 
    ///   1 ---------2  
    ///  /|         /|
    /// 0 |--------3 |
    /// | |        | |
    /// | 5--------|-6
    /// |/         |/
    /// 4 ---------7
    /// </summary>
    /// <param name="facingCamera"></param>
    public Vector3[] Get3DWorldCorners()
    {
        Vector3[] worldspacecorners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            worldspacecorners[i] = localToWorld(objectData.worldBoundingBox[i]);
        }

        return worldspacecorners;
    }


    /// <summary>
    /// Gets a Texture2D version of the 2D mask that displays which pixels within the bounding box a detected object occupies. 
    /// Texture is the size of the 2D bounding box and meant to be overlayed on top of it. 
    /// </summary>
    /// <param name="masktex">Texture2D output - set this to the Texture2D object you want to be the mask.</param>
    /// <param name="fliponYaxis">True to flip the image on the Y axis, since it comes from the ZED upside-down relative to Unity coords.\r\n
    /// Note: It is faster to invert the Y UV value in the shader that displays it, since the bytes must be flipped in a for loop otherwise. </param>
    /// <returns>True if texture was successfully retrieved; false otherwise.</returns>
    public bool GetMaskTexture(out Texture2D masktex, bool fliponYaxis)
    {
        if (!fliponYaxis)
        {
            if (maskTexture == null)
            {
                IntPtr maskpointer = maskMat.GetPtr(sl.ZEDMat.MEM.MEM_CPU);
                if (maskpointer != IntPtr.Zero)
                {
                    maskTexture = ZEDMatToTexture_CPU(maskMat, false);
                }
            }
        }
        else
        {
            if (maskTextureFlipped == null)
            {
                IntPtr maskpointer = maskMat.GetPtr(sl.ZEDMat.MEM.MEM_CPU);
                if (maskpointer != IntPtr.Zero)
                {
                    maskTextureFlipped = ZEDMatToTexture_CPU(maskMat, true);
                }
            }
        }

        masktex = fliponYaxis ? maskTextureFlipped : maskTexture;
        return masktex != null;
    }

    /// <summary>
    /// Frees the memory from all textures and materials that get cached in this object. 
    /// Called from ZEDManager when these are part of a frame that are no longer needed. 
    /// </summary>
    public void CleanUpTextures()
    {
        if (maskTexture != null)
        {
            GameObject.Destroy(maskTexture);
        }

        if (maskTextureFlipped != null)
        {
            GameObject.Destroy(maskTextureFlipped);
        }
    }

    /// <summary>
    /// Transforms 3D points provided from the raw ObjectData values to world space. 
    /// </summary>
    /// <param name="localPos">Any 3D position provided from the raw ObjectData object, like world position or the 3D bbox corners.</param>
    /// <returns>The given position, but in world space.</returns>
    private Vector3 localToWorld(Vector3 localPos)
    {
        return camRotationAtDetection * localPos + camPositionAtDetection;
    }

    /// <summary>
    /// Helper function to 
    /// </summary>
    /// <returns></returns>
    private Quaternion GetRotationWithoutYaw()
    {
        return Quaternion.Euler(camRotationAtDetection.eulerAngles.x, 0f, camRotationAtDetection.eulerAngles.z);
    }

    /// <summary>
    /// Converts the given zedMat to an Alpha8 (8-bit single channel) texture. 
    /// Assumes you are giving it the ZEDMat from an Object Detection mask, and is therefore 8-bit single channel. 
    /// </summary>
    private static Texture2D ZEDMatToTexture_CPU(sl.ZEDMat zedmat, bool flipYcoords = false)
    {
        int width = zedmat.GetWidth(); //Shorthand. 
        int height = zedmat.GetHeight();


        IntPtr maskpointer = zedmat.GetPtr(sl.ZEDMat.MEM.MEM_CPU);
        if (maskpointer != IntPtr.Zero && zedmat.IsInit())
        {
            byte[] texbytes = new byte[zedmat.GetStepBytes() * height];

            System.Runtime.InteropServices.Marshal.Copy(zedmat.GetPtr(sl.ZEDMat.MEM.MEM_CPU), texbytes, 0, texbytes.Length);

            if (flipYcoords)
            {
                byte[] flippedbytes = new byte[texbytes.Length];
                int steplength = zedmat.GetWidthBytes();
                for (int i = 0; i < texbytes.Length; i += steplength)
                {
                    Array.Copy(texbytes, i, flippedbytes, flippedbytes.Length - i - steplength, steplength);
                }

                texbytes = flippedbytes;
            }

            Texture2D zedtex = new Texture2D(width, height, TextureFormat.Alpha8, false, false);
            zedtex.anisoLevel = 0;
            zedtex.LoadRawTextureData(texbytes);
            zedtex.Apply(); //Slight bottleneck here - it forces the CPU and GPU to sync. 

            return zedtex;
        }
        else
        {
            Debug.LogError("Pointer to texture was null - returning null.");
            return null;
        }
    }
}
