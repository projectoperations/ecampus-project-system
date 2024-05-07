﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IProjectTreeActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal partial class RenamerProjectTreeActionHandler : ProjectTreeActionHandlerBase
    {
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsUIService<IVsExtensibility, IVsExtensibility3> _extensibility;
        private readonly IVsOnlineServices _vsOnlineServices;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly Workspace _workspace;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        [ImportingConstructor]
        public RenamerProjectTreeActionHandler(
            UnconfiguredProject unconfiguredProject,
            IUnconfiguredProjectVsServices projectVsServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            IEnvironmentOptions environmentOptions,
            IUserNotificationServices userNotificationServices,
            IRoslynServices roslynServices,
            IWaitIndicator waitService,
            IVsOnlineServices vsOnlineServices,
            IProjectThreadingService threadingService,
            IVsUIService<IVsExtensibility, IVsExtensibility3> extensibility,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
        {
            _unconfiguredProject = unconfiguredProject;
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
            _waitService = waitService;
            _vsOnlineServices = vsOnlineServices;
            _threadingService = threadingService;
            _extensibility = extensibility;
            _operationProgressService = operationProgressService;
            _settingsManagerService = settingsManagerService;
        }

        protected virtual async Task CPSRenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            await base.RenameAsync(context, node, value);
        }

        public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            Requires.NotNull(context, nameof(Context));
            Requires.NotNull(node, nameof(node));
            Requires.NotNullOrEmpty(value, nameof(value));

            string? oldFilePath = node.FilePath;
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newFileWithExtension = value;
            CodeAnalysis.Project? project = GetCurrentProject();

            // Rename the file
            await CPSRenameAsync(context, node, value);

            if (await IsAutomationFunctionAsync() || node.IsFolder || _vsOnlineServices.ConnectedToVSOnline)
            {
                // Do not display rename Prompt
                return;
            }

            if (project is null)
            {
                return;
            }

            string newName = Path.GetFileNameWithoutExtension(newFileWithExtension);
            if (!await CanRenameType(project, oldName, newName))
            {
                return;
            }

            (bool result, Renamer.RenameDocumentActionSet? documentRenameResult) = await GetRenameSymbolsActions(project, oldFilePath, newFileWithExtension);
            if (!result || documentRenameResult == null)
            {
                return;
            }

            // Ask if the user wants to rename the symbol
            bool userWantsToRenameSymbol = await CheckUserConfirmation(oldName);
            if (!userWantsToRenameSymbol)
                return;

            _threadingService.RunAndForget(async () =>
            {
                Solution currentSolution = await PublishLatestSolutionAsync();

                string renameOperationName = string.Format(CultureInfo.CurrentCulture, VSResources.Renaming_Type_from_0_to_1, oldName, value);
                WaitIndicatorResult<Solution> result = _waitService.Run(
                                title: VSResources.Renaming_Type,
                                message: renameOperationName,
                                allowCancel: true,
                                token => documentRenameResult.UpdateSolutionAsync(currentSolution, token));

                // Do not warn the user if the rename was cancelled by the user	
                if (result.IsCancelled)
                {
                    return;
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                if (!_roslynServices.ApplyChangesToSolution(currentSolution.Workspace, result.Result))
                {
                    string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                    _userNotificationServices.ShowWarning(failureMessage);
                }
                return;
            }, _unconfiguredProject);
        }

        private async Task<Solution> PublishLatestSolutionAsync()
        {
            // WORKAROUND: We don't yet have a way to wait for the rename changes to propagate 
            // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
            // instead we wait for the IntelliSense stage to finish for the entire solution
            // 
            IVsOperationProgressStageStatus stageStatus = (await _operationProgressService.GetValueAsync()).GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            await stageStatus.WaitForCompletionAsync();

            // The result of that wait, is basically a "new" published Solution, so grab it
            return _workspace.CurrentSolution;
        }

        private static async Task<(bool, Renamer.RenameDocumentActionSet?)> GetRenameSymbolsActions(CodeAnalysis.Project project, string? oldFilePath, string newFileWithExtension)
        {
            CodeAnalysis.Document? oldDocument = GetDocument(project, oldFilePath);
            if (oldDocument is null)
            {
                return (false, null);
            }

            // Get the list of possible actions to execute
            Renamer.RenameDocumentActionSet documentRenameResult = await Renamer.RenameDocumentAsync(oldDocument, newFileWithExtension);

            // Check if there are any symbols that need to be renamed
            if (documentRenameResult.ApplicableActions.IsEmpty)
            {
                return (false, documentRenameResult);
            }

            // Check errors before applying changes
            if (documentRenameResult.ApplicableActions.Any(a => !a.GetErrors().IsEmpty))
                return (false, documentRenameResult);

            return (true, documentRenameResult);
        }

        private async Task<bool> CanRenameType(CodeAnalysis.Project? project, string oldName, string newName)
        {
            // see if the current project contains a compilation
            (bool success, bool isCaseSensitive) = await TryDetermineIfCompilationIsCaseSensitiveAsync(project);
            if (!success)
            {
                return false;
            }

            if (!CanHandleRename(oldName, newName, isCaseSensitive))
            {
                return false;
            }
            return true;
        }

        private bool CanHandleRename(string oldName, string newName, bool isCaseSensitive)
            => _roslynServices.IsValidIdentifier(oldName) &&
               _roslynServices.IsValidIdentifier(newName) &&
              (!string.Equals(
                  oldName,
                  newName,
                  isCaseSensitive
                    ? StringComparisons.LanguageIdentifiers
                    : StringComparisons.LanguageIdentifiersIgnoreCase));

        private static async Task<(bool success, bool isCaseSensitive)> TryDetermineIfCompilationIsCaseSensitiveAsync(CodeAnalysis.Project? project)
        {
            if (project is null)
                return (false, false);

            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                // this project does not support compilations
                return (false, false);
            }

            return (true, compilation.IsCaseSensitive);
        }

        protected virtual async Task<bool> IsAutomationFunctionAsync()
        {
            await _threadingService.SwitchToUIThread();

            _extensibility.Value.IsInAutomationFunction(out int isInAutomationFunction);
            return isInAutomationFunction != 0;
        }

        private CodeAnalysis.Project? GetCurrentProject() =>
            _workspace.CurrentSolution.Projects.FirstOrDefault(proj => StringComparers.Paths.Equals(proj.FilePath, _projectVsServices.Project.FullPath));

        private static CodeAnalysis.Document GetDocument(CodeAnalysis.Project project, string? filePath) =>
            project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, filePath));

        private async Task<bool> CheckUserConfirmation(string oldFileName)
        {
            ISettingsManager settings = await _settingsManagerService.GetValueAsync();

            bool EnableSymbolicRename = settings.GetValueOrDefault("SolutionNavigator.EnableSymbolicRename", false);

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            bool disablePromptMessage = false;
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);

            if (EnableSymbolicRename && userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

                bool userSelection = _userNotificationServices.Confirm(renamePromptMessage, out disablePromptMessage);

                _environmentOptions.SetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", !disablePromptMessage);

                return userSelection;
            }

            return EnableSymbolicRename;
        }
    }
}
