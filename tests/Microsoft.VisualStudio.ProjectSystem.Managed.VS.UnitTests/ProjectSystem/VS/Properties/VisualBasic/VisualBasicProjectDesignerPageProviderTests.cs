﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    public class VisualBasicProjectDesignerPageProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            CreateInstance();
        }

        [Fact]
        public async Task GetPagesAsync_WhenAllCapabilitiesPresent_ReturnsPagesInOrder()
        {
            var provider = CreateInstance(ProjectCapability.LaunchProfiles, ProjectCapability.Pack);
            var result = await provider.GetPagesAsync();

            var expected = ImmutableArray.Create<IPageMetadata>(
               VisualBasicProjectDesignerPage.Application,
               VisualBasicProjectDesignerPage.Compile,
               VisualBasicProjectDesignerPage.Package,
               VisualBasicProjectDesignerPage.References,
               VisualBasicProjectDesignerPage.Debug,
               VisualBasicProjectDesignerPage.Signing,
               VisualBasicProjectDesignerPage.CodeAnalysis
            );

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoLaunchProfilesCapability_DoesNotContainDebugPage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(VisualBasicProjectDesignerPage.Debug, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoPackCapability_DoesNotContainPackagePage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(VisualBasicProjectDesignerPage.Package, result);
        }

        private static VisualBasicProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            bool containsCapability(string c) => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new VisualBasicProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
