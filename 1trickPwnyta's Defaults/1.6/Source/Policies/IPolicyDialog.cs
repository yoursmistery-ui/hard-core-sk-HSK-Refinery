using UnityEngine;

namespace Defaults.Policies
{
    public interface IPolicyDialog
    {
        string Title { get; }
        string Topic { get; }
        void ResetPolicies();
        void DoWindowContents(Rect inRect);
        Vector2 InitialSize { get; }
    }
}
