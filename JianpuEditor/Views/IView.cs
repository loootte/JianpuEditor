namespace JianpuEditor.Views
{
    /// <summary>
    /// Strict UI initialization protocol for WinForms views.
    /// Call order on Load (last): InitializeBindings → RestoreLayout → ApplyTheme.
    /// </summary>
    public interface IView
    {
        void InitializeBindings(object viewModel);

        void RestoreLayout();

        void ApplyTheme();
    }
}
