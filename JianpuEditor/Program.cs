using System;
using System.Threading;
using System.Windows.Forms;
using JianpuEditor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JianpuEditor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = AppBootstrapper.ConfigureServices();
            Application.Run(services.GetRequiredService<MainForm>());
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLog.Exception("UI 线程未处理异常", e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppLog.Exception("应用程序未处理异常", e.ExceptionObject as Exception);
        }
    }
}
