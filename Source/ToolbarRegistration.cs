using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace KSTS
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(GUI.MODID, GUI.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Statics : MonoBehaviour
    {
        static public Log Log;

        void Awake()
        {
#if DEBUG
            Log = new Log("KSTS", Log.LEVEL.INFO);
#else
            Log = new Log("KSTS", Log.LEVEL.ERROR);
#endif
        }
    }

}