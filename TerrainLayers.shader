//Stewart Gray III
//Unity Terrain Layers | This Shader creates four Texture layers and interpolates between them based on the slope of the terrain object it is attached to. 
//September 2021
Shader "Custom/TerrainLayers"
{
	//Shader Properties
	Properties
	{	

		//Defining an entry for our tint within the Inspector
		_tint("Color (RGB)", Color) = (1.0, 1.0, 1.0, 1.0)

		//Defining entries for our textures in the Inspector
		_layer0("Ground Layer", 2D) = "" {}
		_layer1("Gravel Layer", 2D) = "" {}
		_layer2("Snow Layer" , 2D) = "" {}
		_layer3("Cliff Layer" , 2D) = "" {}

	
		//This coefficient controls how abrupt the snow layer blends with the other layers of the terrain. 
		_fC("Snow Falloff Coefficient", Range(0.5 , 75)) = 10

		//Defining an entry for where our snow layer will begin
		_height0("Snow Formation Height", Range(0.5 , 5)) = 1.35

		//Defining an entry for our slope threshold (in degrees). 
		_slopeThresh("Slope Angle" , Range(25 , 90)) = 30
	}

	//Shader functions
	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma surface surf Standard

		UNITY_DECLARE_TEX2D(_layer0);
		UNITY_DECLARE_TEX2D(_layer1);
		UNITY_DECLARE_TEX2D(_layer2);
		UNITY_DECLARE_TEX2D(_layer3);

		//Falloff coefficient
		float _fC;

		float4 _tint;
		float _height0;
		float _slopeThresh;

		//Defining a struct for all inputs that we will pass into our surface shader
		struct Input{
			//Get the position of the terrain mesh in world space
			float3 worldPos;

			//Get the normal of the terrain mesh in world space. Needed to calculate slope.
			float3 worldNormal;

			//Get the uv data of each texture (by simply pre-fixing with "uv")
			float2 uv_layer0;
			float2 uv_layer1;
			float2 uv_layer2;
			float2 uv_layer3;
		};

		//Defining a struct for each layer comprising our material
		struct Layer{
			float height;
			float4 color;
		};

		//Approximation of an inverse square falloff
		float inverseSquare(float dist){
			float falloff;
			float scalar = 10;
			falloff = scalar * 1 / _fC * pow(dist, 2);
			return falloff;
		}

		void surf (Input i, inout SurfaceOutputStandard o){
			Layer ground;
			Layer gravel;
			Layer snow;
			Layer cliff;

			//Blend Weight for the gravel ramp, nessecary to reduce its influence on the final color ramp
			float bW0 = 0.6;

			//Blend Weight for the final color ramp
			float bW1 = 0.5;

			//Calculating the slope, 0 is completely flat and 1 points completely up
			float slope = 1.0 - i.worldNormal.y;

			//Defining each layer's texure and multiplying by a tint
			ground.color = UNITY_SAMPLE_TEX2D(_layer0, i.uv_layer0) * _tint;
			gravel.color = UNITY_SAMPLE_TEX2D(_layer1, i.uv_layer1) * _tint;
			snow.color = UNITY_SAMPLE_TEX2D(_layer2, i.uv_layer2) * _tint;
			cliff.color = UNITY_SAMPLE_TEX2D(_layer3, i.uv_layer3) * _tint;

			/* 	The snow layer is offset from the top of the mesh
			In the event that our offset goes beneath the mesh, we clamp to 0. */
			snow.height =  max(0, i.worldPos.y -_height0);

			//The gravel is offset at the same height
			gravel.height = snow.height;

			//The snow falloff is calculated by multiplying its elevation by the falloff of its distance from the peak
			float snowFalloff = min(3, snow.height * inverseSquare(_height0));
			
			//The ground and gravel layers will be interpolated up to the mesh's slopes
			float4 groundRamp = lerp(ground.color, (bW0 * gravel.color), slope);

			//The gravel and snow layers will be interpolated up to the snow's elevation
			float4 snowRamp = lerp((bW0 * gravel.color), snow.color, snowFalloff);

			//Blending the ground and snow ramps based on an arbitrary blend weight
			float4 terrainRamp = lerp(groundRamp, snowRamp, bW1);

			/* This final ramp blends between our previous layers and the cliff layer 
			Any slopes in our terrain that are greater than our threshold will not get snow */
			float4 colorRamp = lerp(terrainRamp, cliff.color, slope / radians(_slopeThresh));

			o.Albedo = colorRamp;		
		}

		ENDCG
	}	
}
	