using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;

namespace Defaults.Storyteller
{
    public class DifficultySub : Difficulty
    {
        public Difficulty GetDifficultyValues()
        {
            Difficulty difficulty = new Difficulty();
            foreach (FieldInfo field in typeof(Difficulty).GetFields().Where(f => !f.IsLiteral && f.FieldType != typeof(AnomalyPlaystyleDef)))
            {
                field.SetValue(difficulty, field.GetValue(this));
            }
            typeof(Difficulty).Method("SetMinThreatPointsCurve").Invoke(difficulty, new object[] { });
            return difficulty;
        }

        public void SetDifficultyValues(Difficulty difficulty)
        {
            foreach (FieldInfo field in typeof(Difficulty).GetFields().Where(f => !f.IsLiteral && f.FieldType != typeof(AnomalyPlaystyleDef)))
            {
                field.SetValue(this, field.GetValue(difficulty));
            }
        }
    }
}
