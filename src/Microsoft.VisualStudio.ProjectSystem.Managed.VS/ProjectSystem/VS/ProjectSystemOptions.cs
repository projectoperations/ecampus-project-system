﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";
        private const string UseDesignerByDefaultSettingKey = @"ManagedProjectSystem\UseDesignerByDefault";
        private const string PreferSingleTargetBuildsForStartupProjects = @"ManagedProjectSystem\PreferSingleTargetBuilds";

        // This setting exists as an option in Roslyn repo: 'FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds'.
        // Do not change this setting key unless the Roslyn option name is changed.
        internal const string SkipAnalyzersForImplicitlyTriggeredBuildSettingKey = "TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds";

        private readonly IVsService<ISettingsManager> _settingsManager;
        private readonly IVsUIService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public ProjectSystemOptions(
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager,
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            IProjectThreadingService threadingService)
        {
            _settingsManager = settingsManager;
            _featureFlagsService = featureFlagsService;
            _threadingService = threadingService;
        }

        public Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateEnabledSettingKey, defaultValue: true, cancellationToken);
        }

        public Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateLogLevelSettingKey, defaultValue: LogLevel.None, cancellationToken);
        }

        public Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, defaultValue, cancellationToken);
        }

        public Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken = default)
        {
            return SetSettingValueAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, value, cancellationToken);
        }

        public Task<bool> GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(SkipAnalyzersForImplicitlyTriggeredBuildSettingKey, defaultValue: true, cancellationToken);
        }

        public Task<bool> GetPreferSingleTargetBuildsForStartupProjectsAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(PreferSingleTargetBuildsForStartupProjects, defaultValue: true, cancellationToken);
        }

        private async Task<T> GetSettingValueOrDefaultAsync<T>(string name, T defaultValue, CancellationToken cancellationToken)
        {
            ISettingsManager settingsManager = await _settingsManager.GetValueAsync(cancellationToken);

            return settingsManager.GetValueOrDefault(name, defaultValue);
        }

        private async Task SetSettingValueAsync(string name, object value, CancellationToken cancellationToken)
        {
            ISettingsManager settingsManager = await _settingsManager.GetValueAsync(cancellationToken);

            await settingsManager.SetValueAsync(name, value, isMachineLocal: false);
        }

        public async Task<bool> GetDetectNuGetRestoreCyclesAsync(CancellationToken cancellationToken = default)
        {
            await _threadingService.SwitchToUIThread(cancellationToken);

            IVsFeatureFlags featureFlagsService = _featureFlagsService.Value;

            return featureFlagsService.IsFeatureEnabled(FeatureFlagNames.EnableNuGetRestoreCycleDetection, defaultValue: false);
        }
    }
}
