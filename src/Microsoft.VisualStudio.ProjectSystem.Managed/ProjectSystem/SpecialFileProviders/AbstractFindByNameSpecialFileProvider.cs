﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides <see langword="abstract"/> base class for <see cref="ISpecialFileProvider"/> instances
    ///     that find their special file by file name in the root of the project.
    /// </summary>
    internal abstract class AbstractFindByNameSpecialFileProvider : AbstractSpecialFileProvider
    {
        private readonly string _fileName;

        protected AbstractFindByNameSpecialFileProvider(string fileName, IPhysicalProjectTree projectTree)
            : base(projectTree)
        {
            _fileName = fileName;
        }

        protected override Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            root.TryFindImmediateChild(_fileName, out IProjectTree? node);

            return Task.FromResult(node);
        }

        protected override Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? projectPath = provider.GetRootedAddNewItemDirectory(root);
            if (projectPath == null)  // Root has DisableAddItem
                return Task.FromResult<string?>(null);

            string path = Path.Combine(projectPath, _fileName);

            return Task.FromResult<string?>(path);
        }

        protected string? GetDefaultFileAsync(string rootPath)
        {
            return Path.Combine(rootPath, _fileName);
        }
    }
}
