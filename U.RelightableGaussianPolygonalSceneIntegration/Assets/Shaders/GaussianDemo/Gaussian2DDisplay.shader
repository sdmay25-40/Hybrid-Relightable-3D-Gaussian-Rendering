Shader "RGPSI/Gaussian2DDisplay"
{
    SubShader
    {

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            static const float EULER_NUM = 2.71828;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertOut
            {
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            struct Gaussian3D{

                float3 pos;
            
                float4x4 cov;
                
                float4x4 invCov;
                
                float4 color;
                
                // Index of Spherical harmonics coefficients in buffer
                uint shCoefficientsIndex;
                // Number of spherical harmonics coefficients
                uint shCoefficientsNum;

                float3 normal;

                // PBR Propeties (Color is used for albedo)
                float roughness;
                float metalness;
                float specular;
                float opacity;
                float ambientOcclusion;
                float refraction;
                float emissive;

                /*
                Used like an enum  to denote what type of Gaussian (in the .ply file) this is
                0 = simpleGaussian3D
                1 = gaussian3D
                2 = relightableGaussian3D
                */
                uint gaussianType;
            };

            
            StructuredBuffer<Gaussian3D> gaussians;
            uint numGaussians;
            
            VertOut vert (appdata v)
            {
                // Pass vertex information through
                VertOut o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Use uv coordinate location and Gaussians to determine color
                float4 calculateColor = float4(0, 0, 0, 0);
                for(uint i=0; i < numGaussians; i++){
                    Gaussian3D currGaus = gaussians[i];

                    // Find difference between Gaussian center and uv coordinates of this vertex
                    // NOTE: For the sake of this demo we are assuming the Gaussians are being described in 
                    // UV coordinates.
                    float4 uv4 = float4(v.uv.x, v.uv.y, 0, 1);
                    float4 xt = float4(currGaus.pos.x, currGaus.pos.y, 0, 1) - uv4;
                    float4x1 x = abs(xt);
                    
                    float4x1 tmp = mul(currGaus.invCov, x);
                    float1x1 exponent = -0.5 * mul(transpose(x), tmp);
                    
                    float g = pow(EULER_NUM, exponent[0][0]);
                    calculateColor += currGaus.color * g;
                }

                o.color = calculateColor;

                return o;
            }

            fixed4 frag (VertOut i) : SV_Target
            {
                // Pass through color
                return i.color;
            }
            ENDCG
        }
    }
}
