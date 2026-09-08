using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;

namespace Defaults.Storyteller
{
    public static class StorytellerUtility
    {
        public static Difficulty Copy(this Difficulty difficulty)
        {
            Difficulty copy = new Difficulty();
            foreach (FieldInfo field in typeof(Difficulty).GetDeclaredFields().Where(f => !f.IsLiteral && !f.IsInitOnly))
            {
                field.SetValue(copy, field.GetValue(difficulty));
            }
            return copy;
        }
    }
}
