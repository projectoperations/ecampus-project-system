﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModelMethods.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="ProjectModelActionNames.RemoveLaunchProfile"/> actions.
    /// </summary>
    internal sealed class RemoveLaunchProfileAction : LaunchProfileActionBase
    {
        private readonly RemoveLaunchProfile _executableStep;

        public RemoveLaunchProfileAction(RemoveLaunchProfile executableStep)
        {
            _executableStep = executableStep;
        }

        protected override Task ExecuteAsync(ILaunchSettingsProvider launchSettingsProvider, CancellationToken cancellationToken)
        {
            return launchSettingsProvider.RemoveProfileAsync(_executableStep.ProfileName);
        }
    }
}
