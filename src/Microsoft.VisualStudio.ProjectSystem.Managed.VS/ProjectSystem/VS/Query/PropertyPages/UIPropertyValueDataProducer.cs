﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIPropertyValue"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyValueDataProducer
    {
        public static async Task<IEntityValue> CreateUIPropertyValueValueAsync(IEntityValue parent, ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property, IUIPropertyValuePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                Enumerable.Empty<KeyValuePair<string, string>>());

            return await CreateUIPropertyValueValueAsync(parent.EntityRuntime, identity, configuration, property, requestedProperties);
        }

        public static async Task<IEntityValue> CreateUIPropertyValueValueAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property, IUIPropertyValuePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(property, nameof(property));

            var newUIPropertyValue = new UIPropertyValueValue(runtimeModel, id, new UIPropertyValuePropertiesAvailableStatus());

            if (requestedProperties.UnevaluatedValue)
            {
                if (property is IEvaluatedProperty evaluatedProperty)
                {
                    newUIPropertyValue.UnevaluatedValue = await evaluatedProperty.GetUnevaluatedValueAsync();
                }
                else
                {
                    newUIPropertyValue.UnevaluatedValue = await property.GetValueAsync() as string;
                }
            }

            if (requestedProperties.EvaluatedValue)
            {
                newUIPropertyValue.EvaluatedValue = property switch
                {
                    IBoolProperty boolProperty => await boolProperty.GetValueAsBoolAsync(),
                    IStringProperty stringProperty => await stringProperty.GetValueAsStringAsync(),
                    IIntProperty intProperty => await intProperty.GetValueAsIntAsync(),
                    IEnumProperty enumProperty => (await enumProperty.GetValueAsIEnumValueAsync())?.Name,
                    IStringListProperty stringListProperty => await stringListProperty.GetValueAsStringCollectionAsync(),
                    _ => await property.GetValueAsync()
                };
            }

            ((IEntityValueFromProvider)newUIPropertyValue).ProviderState = new PropertyValueProviderState(configuration, property);

            return newUIPropertyValue;
        }

        public static async Task<IEnumerable<IEntityValue>> CreateUIPropertyValueValuesAsync(
            IQueryExecutionContext executionContext,
            IEntityValue parent,
            IPropertyPageQueryCache cache,
            Rule schema,
            QueryProjectPropertiesContext context,
            string propertyName,
            IUIPropertyValuePropertiesAvailableStatus requestedProperties)
        {
            BaseProperty? unboundProperty = schema.GetProperty(propertyName);
            if (unboundProperty is null)
            {
                return ImmutableList<IEntityValue>.Empty;
            }

            ImmutableList<IEntityValue>.Builder builder = ImmutableList.CreateBuilder<IEntityValue>();

            IEnumerable<ProjectConfiguration> configurations;
            if (unboundProperty.IsConfigurationDependent())
            {
                // Return the values across all configurations.
                configurations = await cache.GetKnownConfigurationsAsync() ?? Enumerable.Empty<ProjectConfiguration>();
            }
            else
            {
                // Return the value from any one configuration.
                if (await cache.GetSuggestedConfigurationAsync() is ProjectConfiguration defaultConfiguration)
                {
                    configurations = CreateSingleItemEnumerable(defaultConfiguration);
                }
                else
                {
                    configurations = Enumerable.Empty<ProjectConfiguration>();
                }
            }

            foreach (ProjectConfiguration configuration in configurations)
            {
                (string versionKey, long versionNumber) = await cache.GetConfiguredProjectVersionAsync(configuration);
                executionContext.ReportInputDataVersion(versionKey, versionNumber);

                if (await cache.BindToRule(configuration, schema.Name, context) is IRule rule
                    && rule.GetProperty(propertyName) is ProjectSystem.Properties.IProperty property)
                {
                    IEntityValue propertyValue = await CreateUIPropertyValueValueAsync(parent, configuration, property, requestedProperties);
                    builder.Add(propertyValue);
                }
            }

            return builder.ToImmutable();

            static IEnumerable<ProjectConfiguration> CreateSingleItemEnumerable(ProjectConfiguration singleProjectConfiguration)
            {
                yield return singleProjectConfiguration;
            }
        }
    }
}
