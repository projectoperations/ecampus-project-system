﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextHost
    {
        /// <summary>
        ///     Responsible for lifetime of a <see cref="IWorkspaceProjectContext"/> and applying changes to a
        ///     project to the context via the <see cref="IApplyChangesToWorkspaceContext"/> service.
        /// </summary>
        internal class WorkspaceProjectContextHostInstance : OnceInitializedOnceDisposedUnderLockAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly IUnconfiguredProjectTasksService _tasksService;
            private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
            private readonly IActiveEditorContextTracker _activeWorkspaceProjectContextTracker;
            private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;
            private readonly IDataProgressTrackerService _dataProgressTrackerService;

            private IDataProgressTrackerServiceRegistration? _evaluationProgressRegistration;
            private IDataProgressTrackerServiceRegistration? _projectBuildProgressRegistration;
            private DisposableBag? _disposables;
            private IWorkspaceProjectContextAccessor? _contextAccessor;
            private ExportLifetimeContext<IApplyChangesToWorkspaceContext>? _applyChangesToWorkspaceContext;

            public WorkspaceProjectContextHostInstance(ConfiguredProject project,
                                                       IProjectThreadingService threadingService,
                                                       IUnconfiguredProjectTasksService tasksService,
                                                       IProjectSubscriptionService projectSubscriptionService,
                                                       IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                                       IActiveEditorContextTracker activeWorkspaceProjectContextTracker,
                                                       IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
                                                       ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory,
                                                       IDataProgressTrackerService dataProgressTrackerService)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectSubscriptionService = projectSubscriptionService;
                _tasksService = tasksService;
                _workspaceProjectContextProvider = workspaceProjectContextProvider;
                _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
                _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
                _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
                _dataProgressTrackerService = dataProgressTrackerService;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _contextAccessor = await _workspaceProjectContextProvider.CreateProjectContextAsync(_project);

                if (_contextAccessor == null)
                    return;

                _activeWorkspaceProjectContextTracker.RegisterContext(_contextAccessor.ContextId);

                _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                _applyChangesToWorkspaceContext.Value.Initialize(_contextAccessor.Context);

                _evaluationProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(WorkspaceProjectContextHostInstance) + ".Evaluation");
                _projectBuildProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(WorkspaceProjectContextHostInstance) + ".ProjectBuild");

                _disposables = new DisposableBag
                {
                    _applyChangesToWorkspaceContext,
                    _evaluationProgressRegistration,
                    _projectBuildProgressRegistration,

                    ProjectDataSources.SyncLinkTo(
                        _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                        _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(GetProjectEvaluationOptions()),
                            target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<ValueTuple<ConfiguredProject, IProjectSubscriptionUpdate>>>(e =>
                                OnProjectChangedAsync(e, evaluation: true),
                                _project.UnconfiguredProject,
                                ProjectFaultSeverity.LimitedFunctionality),
                            linkOptions: DataflowOption.PropagateCompletion,
                            cancellationToken: cancellationToken),

                    ProjectDataSources.SyncLinkTo(
                        _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                        _projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(GetProjectBuildOptions()),
                            target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<ValueTuple<ConfiguredProject, IProjectSubscriptionUpdate>>>(e =>
                                OnProjectChangedAsync(e, evaluation: false),
                                _project.UnconfiguredProject,
                                ProjectFaultSeverity.LimitedFunctionality),
                            linkOptions: DataflowOption.PropagateCompletion,
                            cancellationToken: cancellationToken),

                };
            }

            private StandardRuleDataflowLinkOptions GetProjectEvaluationOptions()
            {
                return DataflowOption.WithRuleNames(_applyChangesToWorkspaceContext!.Value.GetProjectEvaluationRules());
            }

            private StandardRuleDataflowLinkOptions GetProjectBuildOptions()
            {
                return DataflowOption.WithRuleNames(_applyChangesToWorkspaceContext!.Value.GetProjectBuildRules());
            }

            protected override async Task DisposeCoreUnderLockAsync(bool initialized)
            {
                if (initialized)
                {
                    _disposables?.Dispose();

                    if (_contextAccessor != null)
                    {
                        _activeWorkspaceProjectContextTracker.UnregisterContext(_contextAccessor.ContextId);

                        await _workspaceProjectContextProvider.ReleaseProjectContextAsync(_contextAccessor);
                    }
                }
            }

            public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
            {
                CheckForInitialized();

                try
                {
                    await ExecuteUnderLockAsync(_ => action(_contextAccessor!), _tasksService.UnloadCancellationToken);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == DisposalToken)
                {   // We treat cancellation because our instance was disposed differently from when the project is unloading.
                    // 
                    // The former indicates that the active configuration changed, and our ConfiguredProject is no longer 
                    // considered implicitly "active", we throw a different exceptions to let callers handle that.
                    throw new ActiveProjectConfigurationChangedException();
                }
            }

            public async Task<T> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
            {
                CheckForInitialized();

                try
                {
                    return await ExecuteUnderLockAsync(_ => action(_contextAccessor!), _tasksService.UnloadCancellationToken);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == DisposalToken)
                {
                    throw new ActiveProjectConfigurationChangedException();
                }
            }

            internal Task OnProjectChangedAsync(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription)> update, bool evaluation)
            {
                return ExecuteUnderLockAsync(ct => ApplyProjectChangesUnderLockAsync(update, evaluation, ct), _tasksService.UnloadCancellationToken);
            }

            private async Task ApplyProjectChangesUnderLockAsync(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription)> update, bool evaluation, CancellationToken cancellationToken)
            {
                IWorkspaceProjectContext context = _contextAccessor!.Context;
                IProjectVersionedValue<IProjectSubscriptionUpdate> subscription = update.Derive(u => u.subscription);
                bool isActiveEditorContext = _activeWorkspaceProjectContextTracker.IsActiveEditorContext(_contextAccessor.ContextId);
                bool isActiveConfiguration = update.Value.project == _project;

                var state = new ContextState(isActiveEditorContext, isActiveConfiguration);

                context.StartBatch();

                try
                {
                    if (evaluation)
                    {
                        await _applyChangesToWorkspaceContext!.Value.ApplyProjectEvaluationAsync(subscription, state, cancellationToken);
                    }
                    else
                    {
                        await _applyChangesToWorkspaceContext!.Value.ApplyProjectBuildAsync(subscription, state, cancellationToken);
                    }
                }
                finally
                {
                    context.EndBatch();

                    NotifyOutputDataCalculated(update.DataSourceVersions, evaluation);
                }
            }

            private void NotifyOutputDataCalculated(IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions, bool evaluation)
            {
                // Notify operation progress that we've now processed these versions of our input, if they are
                // up-to-date with the latest version that produced, then we no longer considered "in progress".
                if (evaluation)
                {
                    _evaluationProgressRegistration!.NotifyOutputDataCalculated(dataSourceVersions);
                }
                else
                {
                    _projectBuildProgressRegistration!.NotifyOutputDataCalculated(dataSourceVersions);
                }
            }

            private void CheckForInitialized()
            {
                // We should have been initialized by our 
                // owner before they called into us
                Assumes.True(IsInitialized);

                // If we failed to create a context, we treat it as a cancellation
                if (_contextAccessor == null)
                    throw new OperationCanceledException();
            }
        }
    }
}
