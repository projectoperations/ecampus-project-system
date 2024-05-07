﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class UIPropertyValueDataProducerTests
    {
        [Fact]
        public async Task WhenPropertyIsAnIEvaluatedProperty_GetUnevaluatedValueAsyncIsCalled()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: false,
                includeUnevaluatedValue: true);

            var mockEvaluatedProperty = new Mock<IEvaluatedProperty>();
            mockEvaluatedProperty.Setup(m => m.GetUnevaluatedValueAsync()).ReturnsAsync("unevaluated value");
            var property = mockEvaluatedProperty.Object;

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var configuration = ProjectConfigurationFactory.Create(configuration: "Alpha|Beta|Gamma");

            var result = (UIPropertyValueValue)await UIPropertyValueDataProducer.CreateUIPropertyValueValueAsync(
                entityRuntime,
                id,
                configuration,
                property,
                properties);

            Assert.Equal(expected: "unevaluated value", actual: result.UnevaluatedValue);
            mockEvaluatedProperty.Verify(m => m.GetUnevaluatedValueAsync());
        }

        [Fact]
        public async Task WhenThePropertyIsAnIBoolProperty_ThenTheEvaluatedValueIsABool()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: true,
                includeUnevaluatedValue: false);

            var mockBoolProperty = new Mock<IBoolProperty>();
            mockBoolProperty.Setup(m => m.GetValueAsBoolAsync()).ReturnsAsync(true);
            var property = mockBoolProperty.Object;

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var configuration = ProjectConfigurationFactory.Create(configuration: "Alpha|Beta|Gamma");

            var result = (UIPropertyValueValue)await UIPropertyValueDataProducer.CreateUIPropertyValueValueAsync(
                entityRuntime,
                id,
                configuration,
                property,
                properties);

            Assert.Equal(expected: true, actual: result.EvaluatedValue);
        }

        [Fact]
        public async Task WhenThePropertyIsAnIStringProperty_ThenTheEvaluatedValuesIsAString()
        {

            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: true,
                includeUnevaluatedValue: false);

            var mockStringProperty = new Mock<IStringProperty>();
            mockStringProperty.Setup(m => m.GetValueAsStringAsync()).ReturnsAsync("string value");
            var property = mockStringProperty.Object;

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var configuration = ProjectConfigurationFactory.Create(configuration: "Alpha|Beta|Gamma");

            var result = (UIPropertyValueValue)await UIPropertyValueDataProducer.CreateUIPropertyValueValueAsync(
                entityRuntime,
                id,
                configuration,
                property,
                properties);

            Assert.Equal(expected: "string value", actual: result.EvaluatedValue);
        }

        [Fact]
        public async Task WhenThePropertyIsAnIIntProperty_ThenTheEvaluatedValueIsAnInt()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: true,
                includeUnevaluatedValue: false);

            var mockIntProperty = new Mock<IIntProperty>();
            mockIntProperty.Setup(m => m.GetValueAsIntAsync()).ReturnsAsync(42);
            var property = mockIntProperty.Object;

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var configuration = ProjectConfigurationFactory.Create(configuration: "Alpha|Beta|Gamma");

            var result = (UIPropertyValueValue)await UIPropertyValueDataProducer.CreateUIPropertyValueValueAsync(
                entityRuntime,
                id,
                configuration,
                property,
                properties);

            Assert.Equal(expected: 42, actual: result.EvaluatedValue);
        }

        [Fact]
        public async Task WhenThePropertyIsAnIEnumProperty_ThenTheEvaluatedValueIsAString()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: true,
                includeUnevaluatedValue: false);

            var enumValue = IEnumValueFactory.Create(name: "enum value");
            var mockEnumProperty = new Mock<IEnumProperty>();
            mockEnumProperty.Setup(m => m.GetValueAsIEnumValueAsync()).ReturnsAsync(enumValue);
            var property = mockEnumProperty.Object;

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var configuration = ProjectConfigurationFactory.Create(configuration: "Alpha|Beta|Gamma");

            var result = (UIPropertyValueValue)await UIPropertyValueDataProducer.CreateUIPropertyValueValueAsync(
                entityRuntime,
                id,
                configuration,
                property,
                properties);

            Assert.Equal(expected: "enum value", actual: result.EvaluatedValue);
        }
    }
}
