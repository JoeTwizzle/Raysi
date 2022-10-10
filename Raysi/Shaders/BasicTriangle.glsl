#version 460
//#extension GL_ARB_gpu_shader5 : require  
//#extension GL_ARB_bindless_texture : require
const float EPSILON = 1e-8;
const float FloatMax = 3.402823466e+38;
const float MaxDepth = 1000000;
#define localSizeX  8
#define localSizeY  8
#define localSizeZ  1

#define TWO_PI 6.28318530718
#define ONE_OVER_PI (1.0 / PI)
#define ONE_OVER_2PI (1.0 / TWO_PI)
//#define localSize localSizeX * localSizeY * localSizeZ;

layout(rgba16f, binding = 0) uniform image2D screen;
layout(r32f, binding = 1) uniform image2D depth;

layout(binding = 0) uniform sampler2D _TestTex;
layout(binding = 1) uniform sampler2D _Skybox;
struct BVHNode
{
	vec4 bboxA;
	vec4 bboxB;
};

struct Material
{
    uvec2 BaseColorTex;
    uvec2 MetalRoughnessTex;
    uvec2 NormalMapTex;
    uvec2 EmissionTex;
};

struct Vertex
{
 vec3 Pos;
 vec3 Normal;
 vec2 UV;
};

layout (std430, binding = 0) readonly buffer IndexBlock
{
 uint[] Indices;
} IndexBuffer;

layout (std430, binding = 1) readonly buffer VertexBlock
{
 Vertex[] Vertices;
} VertexBuffer;

layout (std430, binding = 2) buffer BVHBlockA
{
	BVHNode[] BVHNodes;
}BVHNodeBuffer;

layout (std430, binding = 3) buffer BVHBlockB
{
	int[] BVHIndices;
}BVHIndexBuffer;

layout (std430, binding = 4) buffer MaterialBlock
{
	Material[] Materials;
}MaterialBuffer;

layout (local_size_x = localSizeX, local_size_y = localSizeY) in;

layout(location = 0) uniform mat4 _MVP;
layout(location = 1) uniform mat4 _CameraToWorld;
layout(location = 2) uniform mat4 _CameraInverseProjection;

struct RayHit
{
   int PrimIndex;
   vec3 Position;
   vec3 Normal;
   vec2 BarycentricCoords;
   float Dist;
};

struct Ray
{
    vec3 origin;
    vec3 direction;
    float tmin;
    float tmax;
};

vec3 hsv2rgb(vec3 c)
{
	vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
	return c.z * mix(K.xxx, clamp(abs(fract(c.x + K.xyz) * 6.0 - K.w) - K.x, 0.0, 1.0), c.y);
}

Ray CreateRay(vec3 origin, vec3 direction)
{
    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    ray.tmax = MaxDepth;
    ray.tmin = 0.0;
    return ray;
}

Ray CreateCameraRay(vec2 rayCoords)
{
    // Transform the camera origin to world space
    const vec3 origin = (_CameraToWorld * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    
    // Invert the perspective projection of the view-space position
    vec3 direction = (_CameraInverseProjection * vec4(rayCoords, 0.0, 1.0)).xyz;
    // Transform the direction from camera to world space and normalize
    direction = (_CameraToWorld * vec4(direction, 0.0)).xyz;
    direction = normalize(direction);
    return CreateRay(origin, direction);
}
//


float Tonemap_ACES(float x)
{
    // Narkowicz 2015, "ACES Filmic Tone Mapping Curve"
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return (x * (a * x + b)) / (x * (c * x + d) + e);
}


const float gamma = 2.2;


//vec3[] data = { vec3(0.0,1.0,0.0),vec3(1.0,0.0,0.0),vec3(-1.0,0.0,0.0),vec3(0.0,1.0,0.0),vec3(1.0,0.0,0.0),vec3(-1.0,0.0,0.0)};
const float PI = 3.14159265;
const int BOUNCES = 2;

vec4 InterpolateVec4(in vec2 barycentricCoords, in vec4 Vector1, in vec4 Vector2, in vec4 Vector3)
{
      return barycentricCoords.x*Vector2+barycentricCoords.y*Vector3+(1-barycentricCoords.x-barycentricCoords.y)*Vector1;
}

vec3 InterpolateVec3(in vec2 barycentricCoords, in vec3 Vector1, in vec3 Vector2, in vec3 Vector3)
{
    return barycentricCoords.x*Vector2+barycentricCoords.y*Vector3+(1-barycentricCoords.x-barycentricCoords.y)*Vector1;
}

vec2 InterpolateVec2(in vec2 barycentricCoords, in vec2 Vector1, in vec2 Vector2, in vec2 Vector3)
{
   return barycentricCoords.x*Vector2+barycentricCoords.y*Vector3+(1-barycentricCoords.x-barycentricCoords.y)*Vector1;
}

float InterpolateFloat(in vec2 barycentricCoords, in float Vector1, in float Vector2, in float Vector3)
{
   return barycentricCoords.x*Vector2+barycentricCoords.y*Vector3+(1-barycentricCoords.x-barycentricCoords.y)*Vector1;
}

bool Intersect(vec3 p0, vec3 p1, vec3 p2, inout Ray ray, inout RayHit hit)
{
    vec3 e1 = p0 - p1;
    vec3 e2 = p2 - p0;
    vec3 n = cross(e1, e2);

    vec3 c = p0 - ray.origin;
    vec3 r = cross(ray.direction, c);
    float inv_det = 1.0 / dot(n, ray.direction);

    float u = dot(r, e2) * inv_det;
    float v = dot(r, e1) * inv_det;
    float w = 1.0 - u - v;

    // These comparisons are designed to return false
    // when one of t, u, or v is a NaN
    if (u >= 0 && v >= 0 && w >= 0)
    {
        float t = dot(n, c) * inv_det;
        if (t >= ray.tmin && t <= ray.tmax)
        {
            ray.tmax = t;
            hit.Dist = t;
            hit.Position = ray.origin + t * ray.direction;
            hit.BarycentricCoords = vec2(u, v);
            return true;
        }
    }

    return false;
}

// bool triIntersect(in vec3 v0, in vec3 v1, in vec3 v2,in Ray ray, inout RayHit hit)
// {
//    return true;
// //    vec3 v1v0 = v1 - v0;
// //    vec3 v2v0 = v2 - v0;
// //    vec3 rov0 = ray.origin - v0;


// //    // The four determinants above have lots of terms in common. Knowing the changing
// //    // the order of the columns/rows doesn't change the volume/determinant, and that
// //    // the volume is dot(cross(a,b,c)), we can precompute some common terms and reduce
// //    // it all to:
// //    vec3  n = cross( v1v0, v2v0 );
// //    vec3  q = cross( rov0, ray.direction );
// //    float d = 1.0/dot( ray.direction, n );
// //    float u = d*dot( -q, v2v0 );
// //    float v = d*dot(  q, v1v0 );
// //    float t = d*dot( -n, rov0 );

// //    //if( u<0.0 || v<0.0 || (u+v)>1.0 ) t = -1.0;
// //    ray.tmax = t;
// //    hit.Dist = t;
// //    hit.Position = ray.origin + t * ray.direction;
// //    hit.BarycentricCoords = vec2(u, v);
// //    return t >= ray.tmin && t <= ray.tmax;
// }

bool Intersect(vec3 p0, vec3 p1, vec3 p2, inout Ray ray)
{
    vec3 e1 = p0 - p1;
    vec3 e2 = p2 - p0;
    vec3 n = cross(e1, e2);

    vec3 c = p0 - ray.origin;
    vec3 r = cross(ray.direction, c);
    float inv_det = 1.0 / dot(n, ray.direction);

    float u = dot(r, e2) * inv_det;
    float v = dot(r, e1) * inv_det;


    // These comparisons are designed to return false
    // when one of t, u, or v is a NaN
    if (u >= 0 && v >= 0 &&  (1.0 - u - v) >= 0)
    {
        float t = dot(n, c) * inv_det;
        if (t >= ray.tmin && t <= ray.tmax)
        {
            ray.tmax = t;
            return true;
        }
    }

    return false;
}

vec3 InvertRayDir(in vec3 dir)
{
    return 
    vec3(
    ((abs(dir.x) <= 0.000001) ? (1.0 / dir.x) : (999999999.0 * (dir.x >= 0.0 ? 1.0 : -1.0))),
    ((abs(dir.y) <= 0.000001) ? (1.0 / dir.y) : (999999999.0 * (dir.y >= 0.0 ? 1.0 : -1.0))),
    ((abs(dir.z) <= 0.000001) ? (1.0 / dir.z) : (999999999.0 * (dir.z >= 0.0 ? 1.0 : -1.0)))
    ); 
}


bool IntersectNode(in Ray ray, in vec3 bboxMin, in vec3 bboxMax)
{
    vec3 inv_dir = 1.0 / (ray.direction);
    vec3 tmin_temp = (bboxMin - ray.origin) * inv_dir;
    vec3 tmax_temp = (bboxMax - ray.origin) * inv_dir;
    vec3 tmin = min(tmin_temp, tmax_temp);
    vec3 tmax = max(tmin_temp, tmax_temp);
    
    float intersectMin = max(tmin.x, max(tmin.y, max(tmin.z, ray.tmin)));
    float intersectMax = min(tmax.x, min(tmax.y, min(tmax.z, ray.tmax)));
    return intersectMin <= intersectMax;
}

RayHit TraverseBVH(inout Ray ray)
{
    RayHit hit;
    hit.PrimIndex = -1;
    hit.Dist = MaxDepth;
    int stack[32];
    int head = 0;

    stack[head++] = 0;
    while (head > 0)
    {
        BVHNode node = BVHNodeBuffer.BVHNodes[stack[--head]];

        if (!IntersectNode(ray, node.bboxA.xyz, vec3(node.bboxA.w, node.bboxB.x, node.bboxB.y)))
            continue;

        int PrimCount = floatBitsToInt(node.bboxB.z);
        int FirstIndex = floatBitsToInt(node.bboxB.w);

        if (PrimCount != 0)
        {
            for (uint i = 0; i < PrimCount; ++i)
            {
                int prim_index = BVHIndexBuffer.BVHIndices[FirstIndex + i];
                Vertex v1 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 0]];
                Vertex v2 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 1]];
                Vertex v3 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 2]];
                if(Intersect(v1.Pos, v2.Pos, v3.Pos, ray, hit))
                {
                    hit.Normal = normalize(InterpolateVec3(hit.BarycentricCoords, v1.Normal, v2.Normal, v3.Normal));
                    hit.PrimIndex = prim_index;
                }
            }
        }
        else
        {
            stack[head++] = FirstIndex;
            stack[head++] = FirstIndex + 1;
        }
    }
    return hit;
}

bool TraverseBVHAny(inout Ray ray)
{
    int stack[32];
    int head = 0;

    stack[head++] = 0;
    while (head > 0)
    {
        BVHNode node = BVHNodeBuffer.BVHNodes[stack[--head]];

        if (!IntersectNode(ray, node.bboxA.xyz, vec3(node.bboxA.w, node.bboxB.x, node.bboxB.y)))
            continue;

        int PrimCount = floatBitsToInt(node.bboxB.z);
        int FirstIndex = floatBitsToInt(node.bboxB.w);

        if (PrimCount != 0)
        {
            for (uint i = 0; i < PrimCount; ++i)
            {
                int prim_index = BVHIndexBuffer.BVHIndices[FirstIndex + i];
                Vertex v1 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 0]];
                Vertex v2 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 1]];
                Vertex v3 = VertexBuffer.Vertices[IndexBuffer.Indices[prim_index * 3 + 2]];
                if(Intersect(v1.Pos, v2.Pos, v3.Pos, ray))
                {
                    return true;
                }
            }
        }
        else
        {
            stack[head++] = FirstIndex;
            stack[head++] = FirstIndex + 1;
        }
    }
    return false;
}
uvec3 pcg3d(uvec3 v) {
  v = v * 1664525u + 1013904223u;
  v.x += v.y * v.z;
  v.y += v.z * v.x;
  v.z += v.x * v.y;
  v ^= v >> 16u;
  v.x += v.y * v.z;
  v.y += v.z * v.x;
  v.z += v.x * v.y;
  return v;
}

vec3 randomCosineWeightedHemispherePoint(vec3 rand, vec3 n) {
  float r = rand.x * 0.5 + 0.5; // [-1..1) -> [0..1)
  float angle = (rand.y + 1.0) * PI; // [-1..1] -> [0..2*PI)
  float sr = sqrt(r);
  vec2 p = vec2(sr * cos(angle), sr * sin(angle));
  /*
   * Unproject disk point up onto hemisphere:
   * 1.0 == sqrt(x*x + y*y + z*z) -> z = sqrt(1.0 - x*x - y*y)
   */
  vec3 ph = vec3(p.xy, sqrt(1.0 - p*p));
  /*
   * Compute some arbitrary tangent space for orienting
   * our hemisphere 'ph' around the normal. We use the camera's up vector
   * to have some fix reference vector over the whole screen.
   */
  vec3 tangent = normalize(rand);
  vec3 bitangent = cross(tangent, n);
  tangent = cross(bitangent, n);
  
  /* Make our hemisphere orient around the normal. */
  return tangent * ph.x + bitangent * ph.y + n * ph.z;
}
const int bounces = 1;
void main() 
{
    const vec3 sunDir = normalize(vec3(2, 1.1, -2));
    ivec2 resolution = imageSize(screen);
    ivec2 pixelPos = ivec2(gl_GlobalInvocationID.xy);
    // Transform pixel to [-1,1] range
    vec2 uv = vec2((pixelPos + vec2(0.5, 0.5)) / vec2(resolution.x, resolution.y));
    vec2 rayCoords = uv * 2.0 - 1.0;
    rayCoords = vec2(rayCoords.x, rayCoords.y);
	Ray ray = CreateCameraRay(rayCoords);
    vec3 color = vec3(0);
    for (int bounce = 0; bounce < bounces + 1; ++bounce)
    {
        float theta = acos(ray.direction.y) / -PI;
        float phi = atan(ray.direction.x, -ray.direction.z) / -PI * 0.5;
        vec3 currentColor = texture2D(_Skybox, vec2(phi, 1 - theta)).rgb;
        if(dot(ray.direction, sunDir) > 1-0.01)
        {
            currentColor = vec3(252/255.0, 236/255.0, 3/255.0);
        }
        float strength = max((1 / (bounce * 2.5 + 1)), 0);
        RayHit hit = TraverseBVH(ray);
        if (hit.PrimIndex == -1)
        {        
            color += currentColor * strength;
            break;
        }
        vec3 faceColor = (hit.Normal * 0.5 + 0.5);
        currentColor = mix(faceColor, vec3(0,1,1), mix(0, 0.5, clamp(hit.Dist / 60.0, 0.0, 1.0)));
        Ray ShadowRay;
        ShadowRay.origin = hit.Position + hit.Normal * 0.001;
        ShadowRay.direction = sunDir;
        ShadowRay.tmin = 0;
        ShadowRay.tmax = MaxDepth;
        float brightness = (dot(hit.Normal, sunDir) + 1) * 0.5;
        brightness *= TraverseBVHAny(ShadowRay) ? 0.5 : 1;
        color += currentColor * brightness * strength;
        //Setup next bounce
        if(bounce < bounces){
            ray.origin = hit.Position + hit.Normal * 0.001;
            ray.direction = normalize(reflect(ray.direction, hit.Normal));
            ray.tmin = 0;
            ray.tmax = MaxDepth;
        }
    }
    
    imageStore(screen, pixelPos, vec4(color, 1.0));    
}
