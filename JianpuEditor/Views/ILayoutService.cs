namespace JianpuEditor.Views
{
    public interface ILayoutService
    {
        void Attach(MainFormLayoutContext context);

        void RestoreLayout();

        void ApplyTheme();

        void ApplyDpiScaling();

        void EnforceZOrder();
    }
}