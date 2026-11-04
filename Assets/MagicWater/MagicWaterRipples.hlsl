#ifndef MAGIC_WATER_RIPPLES_INCLUDED
#define MAGIC_WATER_RIPPLES_INCLUDED

static const int Max = 16;
float4 _MagicWaterRipples[Max]; // xyz = pozycja kroku, w = czas startu
float _MagicWaterRippleCount;


void MagicRipples_float(float3 position, float time, float speed, float lifetime, float width, out float ring,
                        out float2 offset)
{
    ring = 0;
    offset = 0;

    for (int i = 0; i < Max; i++)
    {
        if (i >= (int)_MagicWaterRippleCount) break;

        float4 ripple = _MagicWaterRipples[i];
        float age = time - ripple.w;
        if (age < 0 || age > lifetime) continue;

        float2 toP = position.xz - ripple.xz;
        float dist = length(toP);
        float radius = age * speed;
        float fade = 1 - age / lifetime;

        float mainRing = saturate(1 - abs(dist - radius) / width);
        float secondaryRing = saturate(1 - abs(dist - radius * 0.6) / width * 0.7);
        float flash = exp(-dist * dist * 30) * saturate(1 - age * 4);

        float v = (mainRing * mainRing + secondaryRing * secondaryRing * 0.5) * fade;

        ring += v + flash;
        offset += toP / max(dist, 1e-4) * v;

        /*
         * 1e-4 to 0.0001, mała liczba zabezpieczająca.
         * W samym środku kręgu dist = 0,
         * a dzielenie przez 0 dałoby błąd (NaN, czarne lub migające piksele).
         * max pilnuje, żeby dzielnik nigdy nie był mniejszy niż 0.0001
         */
    }
}


#endif
