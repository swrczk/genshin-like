using UnityEngine;

namespace MagicWater
{
    public static class MagicWaterRipples
    {
        public const int Max = 16; // musi się zgadzać z rozmiarem tablicy w HLSL

        private static readonly Vector4[] S_DATA = new Vector4[Max];
        private static int _sNext;
        private static int _sCount;

        private static readonly int DATA_ID = Shader.PropertyToID("_MagicWaterRipples");
        private static readonly int COUNT_ID = Shader.PropertyToID("_MagicWaterRippleCount");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAll()
        {
            _sNext = 0;
            _sCount = 0;
            System.Array.Clear(S_DATA, 0, Max);
            Shader.SetGlobalVectorArray(DATA_ID, S_DATA);
            Shader.SetGlobalFloat(COUNT_ID, 0);
        }

        public static void Add(Vector3 position)
        {
            S_DATA[_sNext] = new Vector4(position.x, position.y, position.z, Time.timeSinceLevelLoad);
            //Time.timeSinceLevelLoad - ten sam czas, który daje węzeł Time w shaderze
            _sNext = (_sNext + 1) % Max;
            _sCount = Mathf.Min(_sCount + 1, Max);
            Shader.SetGlobalVectorArray(DATA_ID, S_DATA);
            Shader.SetGlobalFloat(COUNT_ID, _sCount);
        }
    }
}