﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="ISupportedValue"/> instances and populating the requested members.
    /// </summary>
    internal static class SupportedValueDataProducer
    {
        public static IEntityValue CreateSupportedValue(IEntityRuntimeModel runtimeModel, ProjectSystem.Properties.IEnumValue enumValue, ISupportedValuePropertiesAvailableStatus requestedProperties)
        {
            var newSupportedValue = new SupportedValueValue(runtimeModel, new SupportedValuePropertiesAvailableStatus());

            if (requestedProperties.DisplayName)
            {
                newSupportedValue.DisplayName = enumValue.DisplayName;
            }

            if (requestedProperties.Value)
            {
                newSupportedValue.Value = enumValue.Name;
            }

            return newSupportedValue;
        }

        public static async Task<IEnumerable<IEntityValue>> CreateSupportedValuesAsync(IEntityRuntimeModel runtimeModel, ProjectSystem.Properties.IEnumProperty enumProperty, ISupportedValuePropertiesAvailableStatus requestedProperties)
        {
            ReadOnlyCollection<ProjectSystem.Properties.IEnumValue> enumValues = await enumProperty.GetAdmissibleValuesAsync();

            return createSupportedValues();

            IEnumerable<IEntityValue> createSupportedValues()
            {
                foreach (ProjectSystem.Properties.IEnumValue value in enumValues)
                {
                    IEntityValue supportedValue = CreateSupportedValue(runtimeModel, value, requestedProperties);
                    yield return supportedValue;
                }
            }
        }
    }
}
