using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace JC.DragSplitStacks
{
    // Gra wywoluje InitMod raz przy ladowaniu moda (klient, serwer dedykowany i host).
    public class Init : IModApi
    {
        public const string Tag = "[JC-DragSplitStacks]";

        public void InitMod(Mod _modInstance)
        {
            Log.Out($"{Tag} Ladowanie z {_modInstance.Path}");
            var harmony = new Harmony("jc.JC-DragSplitStacks");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out($"{Tag} Zaladowano, zalatanych metod: {harmony.GetPatchedMethods().Count()}");
        }
    }
}
