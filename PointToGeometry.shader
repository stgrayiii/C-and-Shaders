//Stewart Gray III
//Point to Geometry Shader | Takes the valid points from the point cloud, transforms them into quads, and advects them via simplex noise
//August 2017
Shader "Unlit/PointCloud"
{
	Properties
	{
		_MainTex("Texture (RGB)", 2D) = "white" {}
		_Size("Size", Float) = 0.1
		_NoiseScale("Noise Scale", Float) = 0.1
		_NoiseStrength("Noise Strength", Float) = 50.0
	}

	SubShader
	{
		Tags{ "Queue" = "AlphaTest" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		//Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Blend SrcAlpha OneMinusSrcAlpha
		//AlphaToMask On
		Cull Off
		ZWrite Off
		Pass

	{

	CGPROGRAM
	#pragma vertex vert
	#pragma geometry geomQuad
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "SimplexNoise3D.hlsl"


				sampler2D _MainTex;
				float _Size;
				float _NoiseScale;
				float _NoiseStrength;

				//Creating a struct for our point - the pieces that comprise our point cloud
				struct Point
				{
					float3 position;
					float3 velocity;
					float2 uv;
					float4 color;

				};

				struct GS_INPUT
				{
					float4 vertex : POSITION;
					float3 normal	: NORMAL;
					float4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
					float2 texcoord1 : TEXCOORD1;
				};

				struct FS_INPUT {
					float4 vertex : SV_POSITION;
					float3 normal : NORMAL;
					float4 color : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				StructuredBuffer<Point> pBuffer;

				float3 curl(float x, float y, float z) {
					float eps = 1.0f;
					float n1, n2, a, b;
					float dt;
					float3 curl;

					//Calculating the change of X
					n1 = snoise(float3(x, y + eps, z));
					n2 = snoise(float3(x, y - eps, z));
					a = (n1 - n2) / (2.0f * eps);

					n1 = snoise(float3(x, y, z + eps));
					n2 = snoise(float3(x, y, z - eps));
					b = (n1 - n2) / (2.0f * eps);

					curl.x = a - b;

					//Calculating the change of Y
					n1 = snoise(float3(x, y, z + eps));
					n2 = snoise(float3(x, y, z - eps));
					a = (n1 - n2) / (2.0f * eps);

					n1 = snoise(float3(x + eps, y, z));
					n2 = snoise(float3(x + eps, y, z));
					b = (n1 - n2) / (2.0f * eps);


					curl.y = a - b;

					//Calculating the change of Z
					n1 = snoise(float3(x + eps, y, z));
					n2 = snoise(float3(x - eps, y, z));
					a = (n1 - n2) / (2.0f * eps);


					n1 = snoise(float3(x, y + eps, z));
					n2 = snoise(float3(x, y - eps, z));
					b = (n1 - n2) / (2.0f * eps);


					curl.z = a - b;

					return curl;

				}

				GS_INPUT vert(appdata_full v)
				{
					GS_INPUT o = (GS_INPUT)0;
					float4 pos = v.vertex;

					float3 noiseLookup = float3(pos.xz  * _NoiseScale, _Time.y);
					float4 newPos = pos +  float4(curl(noiseLookup.x, noiseLookup.y, noiseLookup.z) * _NoiseStrength, 1.0f);

					o.vertex = newPos;
					o.texcoord = v.texcoord;
					o.normal = v.normal;
					o.color = v.color;
					return o;
				}

				[maxvertexcount(3)]
				void geomTri(point GS_INPUT tri[1], inout TriangleStream<FS_INPUT> triStream)
				{
					FS_INPUT pIn = (FS_INPUT)0;
					pIn.normal = mul(unity_ObjectToWorld, tri[0].normal);
					pIn.color = tri[0].color;

					float4 vertex = mul(unity_ObjectToWorld, tri[0].vertex);
					float3 tangent = normalize(cross(float3(0,1,0), pIn.normal));
					float3 up = normalize(cross(tangent, pIn.normal));

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(tangent * -_Size / 1.5, 0));
					pIn.texcoord = float2(-0.5,0);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(up * _Size, 0));
					pIn.texcoord = float2(0.5,1.5);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(tangent * _Size / 1.5, 0));
					pIn.texcoord = float2(1.5,0);
					triStream.Append(pIn);
				}

				[maxvertexcount(6)]
				void geomQuad(point GS_INPUT tri[1], inout TriangleStream<FS_INPUT> triStream)
				{
					FS_INPUT pIn = (FS_INPUT)0;
					pIn.normal = mul(unity_ObjectToWorld, tri[0].normal);
					pIn.color = tri[0].texcoord.x;

					float4 vertex = mul(unity_ObjectToWorld, tri[0].vertex);
					float3 tangent = normalize(cross(float3(0, 1, 0), pIn.normal));
					float3 up = normalize(cross(tangent, pIn.normal));

					// First Tri in quad
					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(-1, -1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(0, 0);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(-1, 1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(0, 1);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(1, 1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(1, 1);
					triStream.Append(pIn);

					// Second Tri in quad
					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(1, 1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(1, 1);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(1, -1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(1, 0);
					triStream.Append(pIn);

					pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(-1, -1, 0, 0) * _Size * 0.5);
					pIn.texcoord = float2(0, 0);
					triStream.Append(pIn);



				}

				float4 frag(FS_INPUT i) : COLOR
				{
					float4 color = float4(0,0,0.05,0.5);

					float alpha = saturate(i.color.x);
					color.a = alpha;
					return color;
				}
					ENDCG
				}
		}
}
