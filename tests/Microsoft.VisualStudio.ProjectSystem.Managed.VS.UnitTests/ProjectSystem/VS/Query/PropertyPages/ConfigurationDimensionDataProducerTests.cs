﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class ConfigurationDimensionDataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateConfigurationDimensionAvailableStatus(
                includeName: true,
                includeValue: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var dimension = new KeyValuePair<string, string>("AlphaDimension", "AlphaDimensionValue");

            var result = (ConfigurationDimensionValue)ConfigurationDimensionDataProducer.CreateProjectConfigurationDimension(entityRuntime, dimension, properties);

            Assert.Equal(expected: "AlphaDimension", actual: result.Name);
            Assert.Equal(expected: "AlphaDimensionValue", actual: result.Value);
        }

        [Fact]
        public void WhenPropertiesAreNotRequested_PropertyValuesAreNotReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateConfigurationDimensionAvailableStatus(
                includeName: false,
                includeValue: false);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var dimension = new KeyValuePair<string, string>("AlphaDimension", "AlphaDimensionValue");

            var result = (ConfigurationDimensionValue)ConfigurationDimensionDataProducer.CreateProjectConfigurationDimension(entityRuntime, dimension, properties);

            Assert.Throws<MissingDataException>(() => result.Name);
            Assert.Throws<MissingDataException>(() => result.Value);
        }

        [Fact]
        public void WhenCreatingEntitiesFromAProjectConfiguration_OneEntityIsCreatedPerDimension()
        {
            var properties = PropertiesAvailableStatusFactory.CreateConfigurationDimensionAvailableStatus(
                includeName: true,
                includeValue: true);

            var entityRuntimeModel = IEntityRuntimeModelFactory.Create();
            var configuration = ProjectConfigurationFactory.Create("Alpha|Beta|Gamma", "A|B|C");
            var results = ConfigurationDimensionDataProducer.CreateProjectConfigurationDimensions(entityRuntimeModel, configuration, properties);

            // We can't guarantee an order for the dimensions, so just check that all the expected values are present.
            Assert.Contains(results, entity => entity is ConfigurationDimensionValue dimension && dimension.Name == "Alpha" && dimension.Value == "A");
            Assert.Contains(results, entity => entity is ConfigurationDimensionValue dimension && dimension.Name == "Beta" && dimension.Value == "B");
            Assert.Contains(results, entity => entity is ConfigurationDimensionValue dimension && dimension.Name == "Gamma" && dimension.Value == "C");
        }
    }
}
