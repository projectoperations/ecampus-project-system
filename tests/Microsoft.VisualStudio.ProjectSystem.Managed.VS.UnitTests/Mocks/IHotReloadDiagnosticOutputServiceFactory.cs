﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IHotReloadDiagnosticOutputServiceFactory
    {
        public static IHotReloadDiagnosticOutputService Create(Action<string>? writeLineCallback = null)
        {
            var mock = new Mock<IHotReloadDiagnosticOutputService>();

            if (writeLineCallback is not null)
            {
                mock.Setup(service => service.WriteLine(It.IsAny<string>()))
                    .Callback(writeLineCallback);
            }

            return mock.Object;
        }
    }
}
