#ifndef REVERBAT_ECHO_REVEAL
#define REVERBAT_ECHO_REVEAL
int _ReverbatPulseCount;
float _ReverbatMaskEnabled;
float4 _ReverbatPulseData[8]; // origin.xy, age, speed
float4 _ReverbatPulseSettings[8]; // duration, fade, feather, maximum radius
float ReverbatVisibility(float2 worldPosition)
{
    if (_ReverbatMaskEnabled < .5) return 1;
    float visibility = 0;
    [unroll] for (int i = 0; i < 8; i++)
    {
        if (i >= _ReverbatPulseCount) break;
        float4 p = _ReverbatPulseData[i];
        float4 s = _ReverbatPulseSettings[i];
        float distanceFromBat = distance(worldPosition, p.xy);
        float radius = min(p.z * p.w, s.w);
        float front = smoothstep(-s.z * .5, s.z * .5, radius - distanceFromBat);
        float localAge = p.z - distanceFromBat / max(p.w, .01);
        float fade = 1 - smoothstep(s.x, s.x + max(s.y, .01), localAge);
        visibility = max(visibility, front * fade);
    }
    return saturate(visibility);
}
#endif
