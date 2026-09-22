// Tinte que MULTIPLICA lo que ya esta dibujado, en vez de pintar encima.
//
// Lo usan las dos capas que oscurecen el mapa: la niebla de guerra y el ciclo del dia.
// Multiplicar y no pintar encima es lo que conserva el dibujo de debajo: un negro con
// transparencia sobre un guerrero azul da un guerrero azul apagado, mientras que pintar
// gris encima da una mancha gris con forma de guerrero. En pixel art la diferencia se ve
// enseguida, porque el contorno negro del pack deja de ser negro.
//
// La otra razon es el ADR-11: la maquina de estados es la unica dueña del color del sprite
// de su unidad. Si la noche se hiciera tiñendo cada SpriteRenderer, la noche y el destello
// de impacto se estarian peleando por el mismo campo y ganaria el ultimo que escribiera.
// Oscureciendo por encima, nadie toca los sprites.
Shader "Tiny Tactics/Tinte multiplicativo"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Mascara", 2D) = "white" {}
        _Color ("Tinte", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        // DstColor * 1 + 0: el resultado es lo que habia, multiplicado por lo que sale de
        // aqui. Blanco no toca nada; 0.5 oscurece a la mitad; negro apaga.
        Blend DstColor Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Salida vert(Entrada v)
            {
                Salida o;
                o.posicion = TransformObjectToHClip(v.posicion.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

                // El alfa dice CUANTO oscurece, no cuanto se ve: con alfa 0 sale blanco, y
                // blanco multiplicado deja el pixel intacto. Asi una celda a la vista no
                // paga ni un cambio de color.
                return half4(lerp(half3(1, 1, 1), c.rgb, c.a), 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
