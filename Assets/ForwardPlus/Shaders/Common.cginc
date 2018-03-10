#define POINT_LIGHT 0
#define SPOT_LIGHT 1
#define DIRECTIONAL_LIGHT 2

struct Plane
{
	float3 normal;
	float d;  //distance from origin
};

Plane ComputePlane(float3 p0, float3 p1, float3 p2)
{
	Plane plane;

	float3 v0 = p1 - p0;
	float3 v2 = p2 - p0;

	plane.normal = normalize(cross(v0, v2));
	plane.d = dot(p0, plane.normal);

	return plane;
}

struct Frustum
{
	Plane planes[4];
};

float4 NDCtoViewSpace(float4 p, float4x4 InverseProjection)
{
	float4 t = mul(InverseProjection, p);
	return t / t.w;
}

struct Sphere
{
	float3 center;
	float radius;
};

struct Light
{
	float3 PositionWorldSpace;
	bool Enabled;
	float3 Color;
	float range;
};

bool SphereInsidePlane(Sphere sphere, Plane plane)
{
	return (dot(plane.normal, sphere.center) - plane.d < -sphere.radius);
}

bool SphereInsideFrustum(Sphere sphere, Frustum frustum, float near, float far)
{
	bool ris = true;

	if (sphere.center.z - sphere.radius > near || sphere.center.z + sphere.radius < far)
		ris = false;	

	for (int i = 0; i < 4; ++i)
	{
		if (SphereInsidePlane(sphere, frustum.planes[i]))
		{
			ris = false;
		}
	}

	return ris;
}