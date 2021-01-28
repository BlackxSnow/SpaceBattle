//
// Description : Array and textureless GLSL 2D/3D/4D simplex 
//               noise functions.
//      Author : Ian McEwan, Ashima Arts.
//  Maintainer : stegu
//     Lastmod : 20201014 (stegu)
//     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//               Distributed under the MIT License. See LICENSE file.
//               https://github.com/ashima/webgl-noise
//               https://github.com/stegu/webgl-noise
// 

float3 mod289(float3 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 mod289(float4 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(float4 x)
{
    return mod289(((x * 34.0) + 1.0) * x);
}

float4 taylorInvSqrt(float4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

float4 grad4(float j, float4 ip)
{
    const float4 ones = float4(1.0, 1.0, 1.0, -1.0);
    float4 p, s;
    p.xyz = floor(frac(j * ip.xyz) * 7.0) * ip.z - 1.0;
    p.w = 1.5 - dot(abs(p.xyz), ones.xyz);
	
	// GLSL: lessThan(x, y) = x < y
	// HLSL: 1 - step(y, x) = x < y
    p.xyz -= sign(p.xyz) * (p.w < 0);
	
    return p;
}

float snoise_float(float3 v)
{
    const float2 C = float2(
		0.166666666666666667, // 1/6
		0.333333333333333333 // 1/3
	);
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);
	
// First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);
	
// Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);
	
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y
	
// Permutations
    i = mod289(i);
    float4 p = permute(
		permute(
			permute(
					i.z + float4(0.0, i1.z, i2.z, 1.0)
			) + i.y + float4(0.0, i1.y, i2.y, 1.0)
		) + i.x + float4(0.0, i1.x, i2.x, 1.0)
	);
	
// Gradients: 7x7 points over a square, mapped onto an octahedron.
// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float n_ = 0.142857142857; // 1/7
    float3 ns = n_ * D.wyz - D.xzx;
	
    float4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p,7*7)
	
    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
	
    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);
	
    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);
	
	//float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
	//float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);
	
    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
	
    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);
	
//Normalise gradients
    float4 norm = rsqrt(float4(
		dot(p0, p0),
		dot(p1, p1),
		dot(p2, p2),
		dot(p3, p3)
	));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;
	
// Mix final noise value
    float4 m = max(
		0.6 - float4(
			dot(x0, x0),
			dot(x1, x1),
			dot(x2, x2),
			dot(x3, x3)
		),
		0.0
	);
    m = m * m;
    return 42.0 * dot(
		m * m,
		float4(
			dot(p0, x0),
			dot(p1, x1),
			dot(p2, x2),
			dot(p3, x3)
		)
	);
}

float snoise(float4 v)
{
    const float4 C = float4(
		0.138196601125011, // (5 - sqrt(5))/20 G4
		0.276393202250021, // 2 * G4
		0.414589803375032, // 3 * G4
	 -0.447213595499958 // -1 + 4 * G4
	);

// First corner
    float4 i = floor(
		v +
		dot(
			v,
			0.309016994374947451 // (sqrt(5) - 1) / 4
		)
	);
    float4 x0 = v - i + dot(i, C.xxxx);

// Other corners

// Rank sorting originally contributed by Bill Licea-Kane, AMD (formerly ATI)
    float4 i0;
    float3 isX = step(x0.yzw, x0.xxx);
    float3 isYZ = step(x0.zww, x0.yyz);
    i0.x = isX.x + isX.y + isX.z;
    i0.yzw = 1.0 - isX;
    i0.y += isYZ.x + isYZ.y;
    i0.zw += 1.0 - isYZ.xy;
    i0.z += isYZ.z;
    i0.w += 1.0 - isYZ.z;

	// i0 now contains the unique values 0,1,2,3 in each channel
    float4 i3 = saturate(i0);
    float4 i2 = saturate(i0 - 1.0);
    float4 i1 = saturate(i0 - 2.0);

	//	x0 = x0 - 0.0 + 0.0 * C.xxxx
	//	x1 = x0 - i1  + 1.0 * C.xxxx
	//	x2 = x0 - i2  + 2.0 * C.xxxx
	//	x3 = x0 - i3  + 3.0 * C.xxxx
	//	x4 = x0 - 1.0 + 4.0 * C.xxxx
    float4 x1 = x0 - i1 + C.xxxx;
    float4 x2 = x0 - i2 + C.yyyy;
    float4 x3 = x0 - i3 + C.zzzz;
    float4 x4 = x0 + C.wwww;

// Permutations
    i = mod289(i);
    float j0 = permute(
		permute(
			permute(
				permute(i.w) + i.z
			) + i.y
		) + i.x
	);
    float4 j1 = permute(
		permute(
			permute(
				permute(
					i.w + float4(i1.w, i2.w, i3.w, 1.0)
				) + i.z + float4(i1.z, i2.z, i3.z, 1.0)
			) + i.y + float4(i1.y, i2.y, i3.y, 1.0)
		) + i.x + float4(i1.x, i2.x, i3.x, 1.0)
	);

// Gradients: 7x7x6 points over a cube, mapped onto a 4-cross polytope
// 7*7*6 = 294, which is close to the ring size 17*17 = 289.
    const float4 ip = float4(
		0.003401360544217687075, // 1/294
		0.020408163265306122449, // 1/49
		0.142857142857142857143, // 1/7
		0.0
	);

    float4 p0 = grad4(j0, ip);
    float4 p1 = grad4(j1.x, ip);
    float4 p2 = grad4(j1.y, ip);
    float4 p3 = grad4(j1.z, ip);
    float4 p4 = grad4(j1.w, ip);

// Normalise gradients
    float4 norm = rsqrt(float4(
		dot(p0, p0),
		dot(p1, p1),
		dot(p2, p2),
		dot(p3, p3)
	));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;
    p4 *= rsqrt(dot(p4, p4));

// Mix contributions from the five corners
    float3 m0 = max(
		0.6 - float3(
			dot(x0, x0),
			dot(x1, x1),
			dot(x2, x2)
		),
		0.0
	);
    float2 m1 = max(
		0.6 - float2(
			dot(x3, x3),
			dot(x4, x4)
		),
		0.0
	);
    m0 = m0 * m0;
    m1 = m1 * m1;
	
    return 49.0 * (
		dot(
			m0 * m0,
			float3(
				dot(p0, x0),
				dot(p1, x1),
				dot(p2, x2)
			)
		) + dot(
			m1 * m1,
			float2(
				dot(p3, x3),
				dot(p4, x4)
			)
		)
	);
}

//static const int permutations[] =
//{
//    151, 160, 137, 91, 90, 15,
//	131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
//	190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
//	88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
//	77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
//	102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
//	135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
//	5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
//	223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
//	129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
//	251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
//	49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
//	138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
//    151, 160, 137, 91, 90, 15,
//	131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
//	190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
//	88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
//	77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
//	102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
//	135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
//	5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
//	223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
//	129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
//	251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
//	49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
//	138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
//};



//float fadefloat(float f)
//{
//    return f * f * f * (f * (f * 6 - 15) + 10);
//}

//float3 fade(float3 pos)
//{
//    float3 result;
//    result.x = fadefloat(pos.x);
//    result.y = fadefloat(pos.y);
//    result.z = fadefloat(pos.z);
//    return result;
//}

//int getHash(int3 pos)
//{
//    return permutations[permutations[permutations[pos.x] + pos.y] + pos.z];
//}


//float grad(int hash, float x, float y, float z)
//{
//    switch (hash & 0xF)
//    {
//        case 0x0:
//            return x + y;
//        case 0x1:
//            return -x + y;
//        case 0x2:
//            return x - y;
//        case 0x3:
//            return -x - y;
//        case 0x4:
//            return x + z;
//        case 0x5:
//            return -x + z;
//        case 0x6:
//            return x - z;
//        case 0x7:
//            return -x - z;
//        case 0x8:
//            return y + z;
//        case 0x9:
//            return -y + z;
//        case 0xA:
//            return y - z;
//        case 0xB:
//            return -y - z;
//        case 0xC:
//            return y + x;
//        case 0xD:
//            return -y + z;
//        case 0xE:
//            return y - x;
//        case 0xF:
//            return -y - z;
//        default:
//            return 0; // never happens
//    }
//}

//float perlinNoise3D(float3 pos)
//{
//    pos += 128;
//    pos.x = pos.x < 0 ? abs(pos.x) : pos.x;
//    pos.y = pos.y < 0 ? abs(pos.y) : pos.y;
//    pos.z = pos.z < 0 ? abs(pos.z) : pos.z;
//    int xIndex = (int) pos.x & 255;
//    int yIndex = (int) pos.y & 255;
//    int zIndex = (int) pos.z & 255;
	
//    float3 unitPos = float3(pos.x - (int) pos.x, pos.y % 1.0, pos.z % 1.0);
//    //return abs(unitPos.x);
//    float3 faded = fade(unitPos);

//    int aaa, aba, aab, abb, baa, bba, bab, bbb;
//    aaa = getHash(int3(xIndex    , yIndex    , zIndex    ));
//    aba = getHash(int3(xIndex    , yIndex + 1, zIndex    ));
//    aab = getHash(int3(xIndex    , yIndex    , zIndex + 1));
//    abb = getHash(int3(xIndex    , yIndex + 1, zIndex + 1));
//    baa = getHash(int3(xIndex + 1, yIndex    , zIndex    ));
//    bba = getHash(int3(xIndex + 1, yIndex + 1, zIndex    ));
//    bab = getHash(int3(xIndex + 1, yIndex    , zIndex + 1));
//    bbb = getHash(int3(xIndex + 1, yIndex + 1, zIndex + 1));
	
//    float x1, x2, y1, y2;
//    x1 = lerp(grad(aaa, unitPos.x, unitPos.y    , unitPos.z), grad(baa, unitPos.x - 1, unitPos.y    , unitPos.z), faded.x);
//    x2 = lerp(grad(aba, unitPos.x, unitPos.y - 1, unitPos.z), grad(bba, unitPos.x - 1, unitPos.y - 1, unitPos.z), faded.x);
//    y1 = lerp(x1, x2, faded.y);
    
//    x1 = lerp(grad(aab, unitPos.x, unitPos.y    , unitPos.z - 1), grad(bab, unitPos.x - 1, unitPos.y    , unitPos.z - 1), faded.x);
//    x2 = lerp(grad(abb, unitPos.x, unitPos.y - 1, unitPos.z - 1), grad(bbb, unitPos.x - 1, unitPos.y - 1, unitPos.z - 1), faded.x);
//    y2 = lerp(x1, x2, faded.y);
    
//    return (lerp(y1, y2, faded.z) + 1) / 2;
//}

void octaveNoise_float(float3 p, int octaves, float persistance, float lunacrity, float scale, float time, out float noise)
{
    noise = 0;
    float amplitude = 1;
    float maxValue = 0;
    for (int i = 0; i <= octaves; i++)
    {
        float n;
        n = snoise(float4(p * scale, time));
        noise += n * amplitude;
        
        maxValue += amplitude;
        
        amplitude *= persistance;
        scale *= lunacrity;
    }
    noise /= maxValue;
}