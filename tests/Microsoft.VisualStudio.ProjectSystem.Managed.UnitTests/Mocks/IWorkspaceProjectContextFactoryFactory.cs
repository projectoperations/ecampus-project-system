﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Moq;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    internal static class IWorkspaceProjectContextFactoryFactory
    {
        public static IWorkspaceProjectContextFactory Create()
        {
            return Mock.Of<IWorkspaceProjectContextFactory>();
        }

        public static IWorkspaceProjectContextFactory ImplementCreateProjectContext(Func<string, string, string, Guid, object, string, string, IWorkspaceProjectContext?> action)
        {
            var mock = new Mock<IWorkspaceProjectContextFactory>();

#pragma warning disable 612,618
            mock.Setup(c => c.CreateProjectContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(action!);
#pragma warning restore 612,618

            return mock.Object;
        }
    }
}
