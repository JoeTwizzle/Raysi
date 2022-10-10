#version 460
layout(location = 0) in vec2 v_UV; 

layout(binding = 0) uniform sampler2D _MainTex;
layout(binding = 1) uniform sampler2D _BlueNoise;
out vec4 color;

vec3 VNoiseCell( in vec2 p )
{
    //Fetch 4 value noises--
    return texture(_MainTex, p).rgb;
}
float brightness(vec3 color) 
{
  return dot(color, vec3(0.299, 0.587, 0.114));
}

#define FXAA_SPAN_MAX 8.0
#define FXAA_REDUCE_MUL   (1.0/FXAA_SPAN_MAX)
#define FXAA_REDUCE_MIN   (1.0/128.0)
#define FXAA_SUBPIX_SHIFT (1.0/4.0)

vec3 FxaaPixelShader(sampler2D tex, vec4 uv)
{

    //vec3 rgbNW = textureLodEXT(tex, uv.zw, 0.0).xyz;
    //vec3 rgbNE = textureLodEXT(tex, uv.zw + vec2(1,0)*rcpFrame.xy, 0.0).xyz;
    //vec3 rgbSW = textureLodEXT(tex, uv.zw + vec2(0,1)*rcpFrame.xy, 0.0).xyz;
    //vec3 rgbSE = textureLodEXT(tex, uv.zw + vec2(1,1)*rcpFrame.xy, 0.0).xyz;
    //vec3 rgbM  = textureLodEXT(tex, uv.xy, 0.0).xyz;

    vec2 rcpFrame = 1/ textureSize(tex,0);
    vec3 rgbNW = texture(tex, uv.zw).rgb;
    vec3 rgbNE = texture(tex, uv.zw + vec2(1,0)*rcpFrame.xy).rgb;
    vec3 rgbSW = texture(tex, uv.zw + vec2(0,1)*rcpFrame.xy).rgb;
    vec3 rgbSE = texture(tex, uv.zw + vec2(1,1)*rcpFrame.xy).rgb;
    vec3 rgbM  = texture(tex, uv.xy).rgb;

    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),
        FXAA_REDUCE_MIN);
    float rcpDirMin = 1.0/(min(abs(dir.x), abs(dir.y)) + dirReduce);

    dir = min(vec2( FXAA_SPAN_MAX,  FXAA_SPAN_MAX),
          max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
          dir * rcpDirMin)) * rcpFrame.xy;

    // vec3 rgbA = (1.0/2.0) * (
    //     textureLodEXT(tex, uv.xy + dir * (1.0/3.0 - 0.5), 0.0).xyz +
    //     textureLodEXT(tex, uv.xy + dir * (2.0/3.0 - 0.5), 0.0).xyz);
    // vec3 rgbB = rgbA * (1.0/2.0) + (1.0/4.0) * (
    //     textureLodEXT(tex, uv.xy + dir * (0.0/3.0 - 0.5), 0.0).xyz +
    //     textureLodEXT(tex, uv.xy + dir * (3.0/3.0 - 0.5), 0.0).xyz);

    vec3 rgbA = (1.0/2.0) * (
        texture(tex, uv.xy + dir * (1.0/3.0 - 0.5)).rgb +
        texture(tex, uv.xy + dir * (2.0/3.0 - 0.5)).rgb);
    vec3 rgbB = rgbA * (1.0/2.0) + (1.0/4.0) * (
        texture(tex, uv.xy + dir * (0.0/3.0 - 0.5)).rgb +
        texture(tex, uv.xy + dir * (3.0/3.0 - 0.5)).rgb);

    float lumaB = dot(rgbB, luma);

    if((lumaB < lumaMin) || (lumaB > lumaMax)) return rgbA;

    return rgbB;
}

void main()
{
    float bn = texture(_BlueNoise, v_UV).r;
    
    //2 seperate trianglar mapped blue noise samples for high quality dither
    vec2 blueNoiseDither = vec2(bn);//vec2(remap_pdf_tri_unity(bn.x), remap_pdf_tri_unity(bn.y));
    vec3 actualColor = texture(_MainTex, v_UV).rgb;
    float colorRef = brightness(actualColor);
    float cdx = abs(dFdx(colorRef));
    float cdy = abs(dFdy(colorRef));
    vec3 f = vec3(0.0);

    float a = exp(-(cdx*cdx*3.1415*2));
    float b = exp(-(cdy*cdy*3.1415*2));
    //8 bit fixed point so adjust sub pixel precision
    float ditherSizeX = ((a) / 512.0);
    float ditherSizeY = ((b) / 512.0);
    vec3 f0 = VNoiseCell(v_UV + (blueNoiseDither)*ditherSizeX);
    vec3 f1 = VNoiseCell(v_UV - (blueNoiseDither)*ditherSizeY);
    f = mix(f0, f1, vec3(0.5));
    vec2 size =textureSize(_MainTex, 0);
//    f = mix(actualColor, f, vec3(0.75));
    vec4 uv = vec4( v_UV, v_UV - ((1/size)* (0.5 + FXAA_SUBPIX_SHIFT)));
    color = vec4(actualColor, 1.0 );
}