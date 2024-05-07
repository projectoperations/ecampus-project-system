﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes explicit, resolved dependencies to implicit if they are not present in the set of known project item specs.
    /// </summary>
    /// <remarks>
    /// Only applies to dependencies whose providers implement <see cref="IProjectDependenciesSubTreeProviderInternal"/>.
    /// </remarks>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class ImplicitDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 130;

        public override void BeforeAddOrUpdate(
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            if (projectItemSpecs != null                                              // must have data
                && !Strings.IsNullOrEmpty(dependency.OriginalItemSpec)
                && !dependency.Implicit                                               // explicit
                && dependency.Resolved                                                // resolved
                && dependency.Flags.Contains(DependencyTreeFlags.Dependency)          // dependency
                && !dependency.Flags.Contains(DependencyTreeFlags.SharedProjectDependency) // except for shared projects
                && !projectItemSpecs.Contains(dependency.OriginalItemSpec)            // is not a known item spec
                && subTreeProviderByProviderType.TryGetValue(dependency.ProviderType, out IProjectDependenciesSubTreeProvider provider)
                && provider is IProjectDependenciesSubTreeProviderInternal internalProvider)
            {
                // Obtain custom implicit icon
                ImageMoniker implicitIcon = internalProvider.ImplicitIcon;

                // Obtain a pooled icon set with this implicit icon
                DependencyIconSet implicitIconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(
                    implicitIcon,
                    implicitIcon,
                    dependency.IconSet.UnresolvedIcon,
                    dependency.IconSet.UnresolvedExpandedIcon);

                context.Accept(
                    dependency.SetProperties(
                        iconSet: implicitIconSet,
                        isImplicit: true,
                        flags: dependency.Flags.Except(DependencyTreeFlags.SupportsRemove)));
                return;
            }

            context.Accept(dependency);
        }
    }
}
