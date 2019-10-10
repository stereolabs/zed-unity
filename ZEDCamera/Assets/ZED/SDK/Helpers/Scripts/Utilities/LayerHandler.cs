using UnityEngine;
#if UNITY_EDITOR
 using UnityEditor;
#endif

 
public class LayerHandler
{

	public static bool[] layer_pack_used = new bool[4];
	private LayerHandler()
	{
		for (int k = 0; k < 4; k++)
			layer_pack_used [k] = false;	
	}
 
	private static LayerHandler _instance = null;

	public static LayerHandler GetInstance()
	{
		if (_instance == null)
		{
		  _instance = new LayerHandler();
		}
		return _instance;
	}


	public enum LAYER_MODE
	{
		LAYER_MODE_LEFT_SCREEN,
		LAYER_MODE_RIGHT_SCREEN,
		LAYER_MODE_LEFT_FINALSCREEN,
		LAYER_MODE_RIGHT_FINALSCREEN
	};

	public enum LAYER_PACK_LAYER_NUMBER
	{
		LAYER_PACK_CAM1_LS = 27,
		LAYER_PACK_CAM1_RS = 28,
		LAYER_PACK_CAM1_LFS = 29,
		LAYER_PACK_CAM1_RFS = 30,

		LAYER_PACK_CAM2_LS = 23,
		LAYER_PACK_CAM2_RS = 24,
		LAYER_PACK_CAM2_LFS = 25,
		LAYER_PACK_CAM2_RFS = 26,

		LAYER_PACK_CAM3_LS = 19,
		LAYER_PACK_CAM3_RS = 20,
		LAYER_PACK_CAM3_LFS = 21,
		LAYER_PACK_CAM3_RFS = 22,

		LAYER_PACK_CAM4_LS = 15,
		LAYER_PACK_CAM4_RS = 16,
		LAYER_PACK_CAM4_LFS = 17,
		LAYER_PACK_CAM4_RFS = 18
	};


	public void setUsed(sl.ZED_CAMERA_ID lp, bool used)
	{
		layer_pack_used [(int)lp] = used;
	}

	public bool getUsed(sl.ZED_CAMERA_ID lp)
	{
		return layer_pack_used [(int)lp];
	}


	public int getLayerNumber(sl.ZED_CAMERA_ID lp, LAYER_MODE mode)
	{
		switch (lp) {
		case sl.ZED_CAMERA_ID.CAMERA_ID_01:
			{
				switch (mode) {
				case LAYER_MODE.LAYER_MODE_LEFT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM1_LS;
				case LAYER_MODE.LAYER_MODE_RIGHT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM1_RS;
				case LAYER_MODE.LAYER_MODE_LEFT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM1_LFS;
				case LAYER_MODE.LAYER_MODE_RIGHT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM1_RFS;
				default :
					return -1;
				}

			}
	    	break;

		case sl.ZED_CAMERA_ID.CAMERA_ID_02:
			{
				switch (mode) {
				case LAYER_MODE.LAYER_MODE_LEFT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM2_LS;
				case LAYER_MODE.LAYER_MODE_RIGHT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM2_RS;
				case LAYER_MODE.LAYER_MODE_LEFT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM2_LFS;
				case LAYER_MODE.LAYER_MODE_RIGHT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM2_RFS;
				default :
					return -1;
				}
			}
			break;

		case sl.ZED_CAMERA_ID.CAMERA_ID_03:
			{
				switch (mode) {
				case LAYER_MODE.LAYER_MODE_LEFT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM3_LS;
				case LAYER_MODE.LAYER_MODE_RIGHT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM3_RS;
				case LAYER_MODE.LAYER_MODE_LEFT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM3_LFS;
				case LAYER_MODE.LAYER_MODE_RIGHT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM3_RFS;
				default :
					return -1;
				}
			}
			break;

		case sl.ZED_CAMERA_ID.CAMERA_ID_04:
			{
				switch (mode) {
				case LAYER_MODE.LAYER_MODE_LEFT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM4_LS;
				case LAYER_MODE.LAYER_MODE_RIGHT_SCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM4_RS;
				case LAYER_MODE.LAYER_MODE_LEFT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM4_LFS;
				case LAYER_MODE.LAYER_MODE_RIGHT_FINALSCREEN:
					return (int)LAYER_PACK_LAYER_NUMBER.LAYER_PACK_CAM4_RFS;
				default :
					return -1;
				}
			}
			break;

		default :
			return -1;
		}
	}




}
 