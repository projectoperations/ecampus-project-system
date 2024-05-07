﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IConfigurationDimension"/> instances and populating the requested
    /// members.
    /// </summary>
    internal static class ConfigurationDimensionDataProducer
    {
        public static IEntityValue CreateProjectConfigurationDimension(
            IEntityRuntimeModel runtimeModel,
            KeyValuePair<string, string> projectConfigurationDimension,
            IConfigurationDimensionPropertiesAvailableStatus requestedProperties)
        {
            var newProjectConfigurationDimension = new ConfigurationDimensionValue(runtimeModel, new ConfigurationDimensionPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newProjectConfigurationDimension.Name = projectConfigurationDimension.Key;
            }

            if (requestedProperties.Value)
            {
                newProjectConfigurationDimension.Value = projectConfigurationDimension.Value;
            }

            return newProjectConfigurationDimension;
        }

        public static IEnumerable<IEntityValue> CreateProjectConfigurationDimensions(IEntityRuntimeModel runtimeModel, ProjectConfiguration configuration, IConfigurationDimensionPropertiesAvailableStatus requestedProperties)
        {
            foreach (KeyValuePair<string, string> dimension in configuration.Dimensions)
            {
                IEntityValue projectConfigurationDimension = CreateProjectConfigurationDimension(runtimeModel, dimension, requestedProperties);
                yield return projectConfigurationDimension;
            }
        }
    }
}
