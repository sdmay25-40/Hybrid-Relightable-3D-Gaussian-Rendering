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

            //#define EULER_NUM = 2.71828


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
                
                //TODO: Expand this to be PBR materials when the .ply is updated
                float4 color;
            };

            
            StructuredBuffer<Gaussian3D> gaussians;
            int numGaussians;

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
                    float4 uv4 = float4(v.uv.x, v.uv.y, 0, 0);
                    float4x1 x = uv4 - float4(currGaus.pos.x, currGaus.pos.y, 0, 0);
                    
                    float4 tmp = currGaus.invCov * x;
                    float4 exponent = -0.5 * (transpose(x) * tmp);
                    
                    float g = pow(2.71828, exponent.x);
                    calculateColor += g * currGaus.color;
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
