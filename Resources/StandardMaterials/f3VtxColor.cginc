#ifndef f3VtxColor_INCLUDED
#define f3VtxColor_INCLUDED


// [RMS] below from UnityStandardInput.cginc


//
// [RMS] define new input vtx structure that has color value
// 
struct VertexInput_f3VC
{
	float4 vertex	: POSITION;
	fixed4 color    : COLOR;			// [RMS] added this
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
	UNITY_INSTANCE_ID
};


//
// [RMS] have to define new version of this function because we have a new structure name
//
float4 TexCoords_f3VC(VertexInput_f3VC v)
{
	float4 texcoord;
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
	return texcoord;
}	



// [RMS] below from UnityStandardCore.cginc





// [RMS] have to duplicate this function because of input struct name
inline half4 VertexGIForward_f3VC(VertexInput_f3VC v, float3 posWorld, half3 normalWorld)
{
	half4 ambientOrLightmapUV = 0;
	// Static lightmaps
#ifndef LIGHTMAP_OFF
	ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	ambientOrLightmapUV.zw = 0;
	// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
#elif UNITY_SHOULD_SAMPLE_SH
#ifdef VERTEXLIGHT_ON
	// Approximated illumination from non-important point lights
	ambientOrLightmapUV.rgb = Shade4PointLights (
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, posWorld, normalWorld);
#endif

	ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, ambientOrLightmapUV.rgb);		
#endif

#ifdef DYNAMICLIGHTMAP_ON
	ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

	return ambientOrLightmapUV;
}




// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)


//
// [RMS] added color field
//
struct VertexOutputForwardBase_f3VC
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	SHADOW_COORDS(6)
		UNITY_FOG_COORDS(7)

	fixed4 color                        : COLOR;		// [RMS] added this for vtx color

		// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
		float3 posWorld					: TEXCOORD8;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
	half3 reflUVW				: TEXCOORD9;
#else
	half3 reflUVW				: TEXCOORD8;
#endif
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};



// [RMS] added one line for color
VertexOutputForwardBase_f3VC vertForwardBase_f3VC (VertexInput_f3VC v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputForwardBase_f3VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase_f3VC, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	o.posWorld = posWorld.xyz;
#endif
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords_f3VC(v);		// [RMS] changed this function call
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndParallax[0].xyz = 0;
	o.tangentToWorldAndParallax[1].xyz = 0;
	o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif
	//We need this for shadow receving
	TRANSFER_SHADOW(o);

	o.ambientOrLightmapUV = VertexGIForward_f3VC(v, posWorld, normalWorld);

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
	o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
	o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
	o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
	o.reflUVW 		= reflect(o.eyeVec, normalWorld);
#endif

	// [RMS] this is the only line we added!
	o.color = v.color;

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}


// [RMS] added multiply by color & alpha
half4 fragForwardBaseInternal_f3VC (VertexOutputForwardBase_f3VC i)
{
	FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW		= i.reflUVW;
#endif

	UnityLight mainLight = MainLight (s.normalWorld);
	half atten = SHADOW_ATTENUATION(i);


	half occlusion = Occlusion(i.tex.xy);
	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
	c.rgb += Emission(i.tex.xy);

	c *= i.color;			// [RMS] multiply by our input color

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	//	return OutputForward (c, s.alpha);
	return OutputForward (c, s.alpha * i.color.a);		// multiply by input alpha (necessary?)
}
half4 fragForwardBase_f3VC (VertexOutputForwardBase_f3VC i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return fragForwardBaseInternal_f3VC(i);
}







// ------------------------------------------------------------------
//  Deferred pass

// [RMS] added color member
struct VertexOutputDeferred_f3VC
{
	float4 pos							: SV_POSITION;
	fixed4 color                        : COLOR;		// [RMS] added
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs			

#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	float3 posWorld						: TEXCOORD6;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
	half3 reflUVW				: TEXCOORD7;
#else
	half3 reflUVW				: TEXCOORD6;
#endif
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

// [RMS] added color line
VertexOutputDeferred_f3VC vertDeferred_f3VC (VertexInput_f3VC v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputDeferred_f3VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred_f3VC, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	o.posWorld = posWorld;
#endif
	o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords_f3VC(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndParallax[0].xyz = 0;
	o.tangentToWorldAndParallax[1].xyz = 0;
	o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif

	o.ambientOrLightmapUV = 0;
#ifndef LIGHTMAP_OFF
	o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#elif UNITY_SHOULD_SAMPLE_SH
	o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
#endif
#ifdef DYNAMICLIGHTMAP_ON
	o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
	o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
	o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
	o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
	o.reflUVW		= reflect(o.eyeVec, normalWorld);
#endif

	// [RMS] transfer color to output
	o.color = v.color;

	return o;
}


// [RMS] added lines to multiply color & alpha by input color i.color
void fragDeferred_f3VC (
	VertexOutputDeferred_f3VC i,
	out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
	out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
	out half4 outNormal : SV_Target2,			// RT2: normal (rgb), --unused, very low precision-- (a) 
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
)
{
#if (SHADER_TARGET < 30)
	outDiffuse = 1;
	outSpecSmoothness = 1;
	outNormal = 0;
	outEmission = 0;
	return;
#endif

	FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
		s.reflUVW		= i.reflUVW;
#endif

	// no analytic lights in this pass
	UnityLight dummyLight = DummyLight (s.normalWorld);
	half atten = 1;

	// only GI
	half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif

	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

	half3 color = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	color *= i.color;		// [RMS] multiply by color
	color += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);

#ifdef _EMISSION
	color += Emission (i.tex.xy);
#endif

#ifndef UNITY_HDR_ON
	color.rgb = exp2(-color.rgb);
#endif

	outDiffuse = half4(s.diffColor * i.color.rgb, occlusion);
	outSpecSmoothness = half4(s.specColor * i.color.rgb, s.oneMinusRoughness);
	outNormal = half4(s.normalWorld*0.5+0.5,1);
	outEmission = half4(color, 1);
}








#endif