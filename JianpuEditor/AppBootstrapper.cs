using System;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace JianpuEditor
{
    internal static class AppBootstrapper
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAppMessenger, AppMessenger>();
            services.AddSingleton<IScoreFileService, ScoreFileServiceAdapter>();
            services.AddTransient<ScoreDocumentViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainForm>();

            return services.BuildServiceProvider();
        }
    }
}
