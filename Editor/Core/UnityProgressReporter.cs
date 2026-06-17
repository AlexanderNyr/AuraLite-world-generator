using UnityEditor;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class UnityProgressReporter : IProgressReporter
    {
        private string _title;

        public UnityProgressReporter(string title)
        {
            _title = title;
        }

        public void Report(string step, float normalized, string detail = null)
        {
            EditorUtility.DisplayProgressBar(_title, $"{step} ({normalized * 100:0}%)" + (detail != null ? $": {detail}" : ""), normalized);
        }

        public IProgressReporter CreateSubProgress(float weight)
        {
            // For now, return a simple sub-progress that doesn't actually nest bar, 
            // since Unity only has one progress bar.
            return this; 
        }
    }
}
