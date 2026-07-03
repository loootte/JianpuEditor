using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JianpuEditor
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [STAThread]
        private static void Main()
        {
            AllocConsole();
            Console.WriteLine("JianpuEditor 调试控制台已启用（指令栈日志）");

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppTheme.Load();

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
