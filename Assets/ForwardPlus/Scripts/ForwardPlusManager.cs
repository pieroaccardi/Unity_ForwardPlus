using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Plane
{
	public Vector3 normal;
	public float d;
}

public class Frustum
{
	public Plane[] planes = new Plane[4];
}

//only point lights at the moment
public struct LightData
{
	public Vector3 worldSpacePosition;
	public float enabled;
	public Vector3 color;
	public float range;

	public LightData(Vector3 WorldSpacePosition, float Enabled, Vector3 Col, float Range)
	{
		worldSpacePosition = WorldSpacePosition;
		enabled = Enabled;
		color = Col;
		range = Range;
	}
}

public class ForwardPlusManager : MonoBehaviour
{
	//PUBLIC FIELDS
	public ComputeShader precomputeFrustums;
	public ComputeShader lightCulling;

    public bool Debug_ShowGrid;
    public Texture2D heatmap;

	//PRIVATE FIELDS
	private readonly int LIGHTS_PER_TILE = 64;

	private ComputeBuffer frustumBuffer;
	private ComputeBuffer lightListBuffer;
	private ComputeBuffer currentLightIndexBuffer;
	private ComputeBuffer lightIndexBuffer;

	private RenderTexture depthTexture;
	private RenderTexture lightsGrid;
	private CommandBuffer commandBuffer;

	private Camera cam;

    private Material debug_showGridMat;
    private Material copyDepthMat;

    //PRIVATE METHODS
    private void Start()
	{
		cam = GetComponent<Camera> ();
		cam.depthTextureMode = DepthTextureMode.Depth;

		//compute the number of frustums
		int num_frustums_x = (int)System.Math.Ceiling(Screen.width / 16.0f);
		int num_frustums_y = (int)System.Math.Ceiling (Screen.height / 16.0f);
		int total_frustums = num_frustums_x * num_frustums_y;

		//initialize buffers
		frustumBuffer = new ComputeBuffer (total_frustums, 64, ComputeBufferType.Default);
		lightListBuffer = new ComputeBuffer (256, 32, ComputeBufferType.Default);
		currentLightIndexBuffer = new ComputeBuffer (1, 4, ComputeBufferType.Default);
		lightIndexBuffer = new ComputeBuffer (LIGHTS_PER_TILE * total_frustums, 4, ComputeBufferType.Default);

		depthTexture = new RenderTexture (Screen.width, Screen.height, 24, RenderTextureFormat.RFloat);
        depthTexture.filterMode = FilterMode.Point;
        depthTexture.name = "DepthTexture";

        lightsGrid = new RenderTexture (num_frustums_x, num_frustums_y, 0, RenderTextureFormat.RGInt, RenderTextureReadWrite.Linear);
        lightsGrid.name = "LightsGrid";
        lightsGrid.filterMode = FilterMode.Point;
        lightsGrid.enableRandomWrite = true;
        lightsGrid.Create();

        //populate the buffer with le list of lights
		LightData[] lightsData = new LightData[256];
		int index = 0;
		Light[] lights = GameObject.FindObjectsOfType<Light>();
		foreach (Light l in lights) 
		{
			if (l.type == LightType.Point) 
			{
				Vector3 col = new Vector3(l.color.r, l.color.g, l.color.b) * l.intensity;
				LightData d = new LightData (l.transform.position, 1, col, l.range);  //world space position

				lightsData [index++] = d;

				if (index >= 256)
					break;
			}
		}
        
        //zero-initialize the remaining elements
		for (int i = index; i < 256; ++i) 
		{
			lightsData [i] = new LightData (Vector3.zero, 0, Vector3.zero, 0);
		}

		lightListBuffer.SetData (lightsData);

		//precompute frustums

		Vector4 data = new Vector4(1.0f / (float)Screen.width, 1.0f / (float)Screen.height, num_frustums_x, num_frustums_y);
		Matrix4x4 matrix = GL.GetGPUProjectionMatrix (Camera.main.projectionMatrix, false);
		matrix = matrix.inverse;

		float[] matrixFloats = new float[] 
		{ 
			matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0], 
			matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1], 
			matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2], 
			matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3] 
		}; 

		precomputeFrustums.SetVector ("data", data);
        precomputeFrustums.SetFloats ("InverseProjection", matrixFloats);
        precomputeFrustums.SetBuffer (0, "frustums", frustumBuffer);

		int dispacth_x = (int)System.Math.Ceiling (num_frustums_x / 16.0f);
		int dispacth_y = (int)System.Math.Ceiling (num_frustums_y / 16.0f);

        precomputeFrustums.Dispatch (0, dispacth_x, dispacth_y, 1);

		//materials
		copyDepthMat = new Material(Shader.Find("ForwardPlus/CopyDepth"));

		debug_showGridMat = new Material(Shader.Find("ForwardPlus/debug_showGrid"));
		debug_showGridMat.SetTexture ("lightsGrid", lightsGrid);
        debug_showGridMat.SetFloat("screenWidth", Screen.width);
        debug_showGridMat.SetFloat("screenHeight", Screen.height);
        debug_showGridMat.SetTexture("heatmap", heatmap);

        lightCulling.SetTexture(0, "lightsGrid", lightsGrid);
        lightCulling.SetTexture(0, "depthBuffer", depthTexture);
        lightCulling.SetBuffer(0, "lights", lightListBuffer);

        //set the lights list globally
        Shader.SetGlobalTexture("g_lightsGrid", lightsGrid);
        Shader.SetGlobalBuffer("g_lightsIndexBuffer", lightIndexBuffer);
        Shader.SetGlobalBuffer("g_lights", lightListBuffer);

        commandBuffer = new CommandBuffer();

        //first get the depth texture
        commandBuffer.name = "ForwardPlus";
		commandBuffer.Blit(null, depthTexture, copyDepthMat);

		//then light culling phase
		commandBuffer.SetComputeBufferParam(lightCulling, 0, "frustums", frustumBuffer);
		commandBuffer.SetComputeBufferParam (lightCulling, 0, "currentIndex", currentLightIndexBuffer);
		commandBuffer.SetComputeBufferParam (lightCulling, 0, "lightsIndexBuffer", lightIndexBuffer);
		commandBuffer.SetComputeVectorParam (lightCulling, "data", data);
		commandBuffer.SetComputeFloatParams (lightCulling, "InverseProjection", matrixFloats);

		commandBuffer.DispatchCompute(lightCulling, 0, num_frustums_x, num_frustums_y, 1);

		cam.AddCommandBuffer (CameraEvent.AfterDepthTexture, commandBuffer);
	}

    private void OnPreRender()
    {
        Matrix4x4 matrix = cam.worldToCameraMatrix;
        float[] matrixFloats = new float[]
        {
            matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0],
            matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1],
            matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2],
            matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3]
        };
        lightCulling.SetFloats("WorldToViewMatrix", matrixFloats);

        uint[] zero = new uint[1];
        zero[0] = 0;
        currentLightIndexBuffer.SetData(zero);
    }

	private void OnRenderImage(RenderTexture src, RenderTexture dest) 
	{
        if (Debug_ShowGrid)
        {
            Graphics.Blit(src, dest, debug_showGridMat);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
	}

}
