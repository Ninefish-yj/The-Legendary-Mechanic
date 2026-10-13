using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 全局Update驱动 - 为需要每帧更新的静态系统提供MonoBehaviour Update
    /// </summary>
    public class SuperMechUpdateDriver : MonoBehaviour
    {
        private static SuperMechUpdateDriver _instance;

        public static SuperMechUpdateDriver Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[SuperMech] UpdateDriver");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SuperMechUpdateDriver>();
                }
                return _instance;
            }
        }

        private void Update()
        {
            // 太空飞行每帧更新
            if (SuperMechSpaceFlight.IsFlying)
            {
                SuperMechSpaceFlight.FlightUpdate(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
