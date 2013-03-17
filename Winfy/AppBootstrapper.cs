﻿using System;
using System.Collections.Generic;
using System.Deployment.Application;
using Caliburn.Micro.TinyIOC;
using Winfy.Core;
using Winfy.ViewModels;
using Caliburn.Micro;
using System.IO;
using NLog;
using System.Diagnostics;
using Winfy.Core.Deployment;

namespace Winfy {
    public sealed class AppBootstrapper : TinyBootstrapper<ShellViewModel> {

        private AppSettings _Settings;
        private AppContracts _Contracts;
        private string _SettingsPath;

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e) {
            base.OnStartup(sender, e);

            //TODO: Find a better way
            if(Process.GetProcessesByName("Winfy").Length > 1)
                Application.Shutdown();
        }

        protected override void Configure() {
            base.Configure();

            _Contracts = new AppContracts();
            _SettingsPath = Path.Combine(_Contracts.SettingsLocation, _Contracts.SettingsFilename);
            _Settings = File.Exists(_SettingsPath)
                            ? Serializer.DeserializeFromJson<AppSettings>(_SettingsPath)
                            : new AppSettings();

            
            Container.Register<AppContracts>(_Contracts);
            Container.Register<AppSettings>(_Settings);
            Container.Register<Core.ILog>(new ProductionLogger());
            Container.Register<AutorunService>(new AutorunService(Container.Resolve<Core.ILog>(), _Settings, _Contracts));
            Container.Register<IWindowManager>(new AppWindowManager(_Settings));
            Container.Register<ISpotifyController>(new SpotifyController(Container.Resolve<Core.ILog>()));
            Container.Register<ICoverService>(new CoverService(_Contracts, Container.Resolve<Core.ILog>()));
            Container.Register<IUpdateService>(new UpdateService(Container.Resolve<Core.ILog>()));
            Container.Register<IUsageTrackerService>(ApplicationDeployment.IsNetworkDeployed
                                                         ? new UsageTrackerService(_Settings, Container.Resolve<Core.ILog>(), _Contracts)
                                                         : new LocalUsageTrackerService(_Settings, Container.Resolve<Core.ILog>(), _Contracts));
        }

        protected override void OnExit(object sender, EventArgs e) {
            base.OnExit(sender, e);
            Serializer.SerializeToJson(_Settings, _SettingsPath);
        }
    }
}