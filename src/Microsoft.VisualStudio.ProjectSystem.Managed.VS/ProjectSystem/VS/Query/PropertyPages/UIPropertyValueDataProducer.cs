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

            ((IEntityValueFromProvider)newUIPropertyValue).ProviderState = (configuration, property);

            return newUIPropertyValue;
        }

        public static async Task<IEnumerable<IEntityValue>> CreateUIPropertyValueValuesAsync(
            IEntityValue parent,
            IPropertyPageQueryCache cache,
            Rule schema,
            string propertyName,
            IUIPropertyValuePropertiesAvailableStatus requestedProperties)
        {
            ImmutableList<IEntityValue>.Builder builder = ImmutableList.CreateBuilder<IEntityValue>();

            if (await cache.GetKnownConfigurationsAsync() is IImmutableSet<ProjectConfiguration> knownConfigurations)
            {
                foreach (ProjectConfiguration knownConfiguration in knownConfigurations)
                {
                    if (await cache.BindToRule(knownConfiguration, schema.Name) is IRule rule
                        && rule.GetProperty(propertyName) is ProjectSystem.Properties.IProperty property)
                    {
                        IEntityValue propertyValue = await CreateUIPropertyValueValueAsync(parent, knownConfiguration, property, requestedProperties);
                        builder.Add(propertyValue);
                    }
                }
            }

            return builder.ToImmutable();
        }
    }
}
