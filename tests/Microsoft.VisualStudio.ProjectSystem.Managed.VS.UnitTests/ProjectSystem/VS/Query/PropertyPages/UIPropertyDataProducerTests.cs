﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class UIPropertyDataProducerTests
    {
        [Fact]
        public void WhenCreatingFromAParentAndProperty_ThePropertyNameIsTheEntityId()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeProperties: false);

            var parentEntity = IEntityWithIdFactory.Create(key: "parent", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty { Name = "MyProperty" };
            var order = 42;

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(parentEntity, cache, property, order, properties);

            Assert.Equal(expected: "MyProperty", actual: result.Id[ProjectModelIdentityKeys.UIPropertyName]);
        }

        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeProperties: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Name = "A",
                DisplayName = "Page A",
                Description = "This is the description for Page A",
                HelpUrl = "https://mypage",
                Category = "general",
                DataSource = new DataSource { HasConfigurationCondition = false }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Equal(expected: "A", actual: result.Name);
            Assert.Equal(expected: "Page A", actual: result.DisplayName);
            Assert.Equal(expected: "This is the description for Page A", actual: result.Description);
            Assert.True(result.ConfigurationIndependent);
            Assert.Equal(expected: "general", actual: result.CategoryName);
            Assert.Equal(expected: 42, actual: result.Order);
            Assert.Equal(expected: "string", actual: result.Type);
        }

        [Fact]
        public void WhenTheEntityIsCreated_TheProviderStateIsTheExpectedType()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeProperties: false);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Name = "A"
            };
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.Add(property);
            rule.EndInit();

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.IsType<(IPropertyPageQueryCache, Rule, string)>(((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingPropertiesFromARule_OneEntityIsCreatedPerProperty()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeProperties: true);

            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "ParentRule");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.AddRange(new[]
            {
                new TestProperty { Name = "Alpha" },
                new TestProperty { Name = "Beta" },
                new TestProperty { Name = "Gamma" },
            });
            rule.EndInit();

            var result = UIPropertyDataProducer.CreateUIPropertyValues(parentEntity, cache, rule, properties);

            Assert.Collection(result, new Action<IEntityValue>[]
            {
                entity => { assertEqual(entity, expectedName: "Alpha"); },
                entity => { assertEqual(entity, expectedName: "Beta"); },
                entity => { assertEqual(entity, expectedName: "Gamma"); }
            });

            static void assertEqual(IEntityValue entity, string expectedName)
            {
                var propertyEntity = (UIPropertyValue)entity;
                Assert.Equal(expectedName, propertyEntity.Name);
            }
        }

        private class TestProperty : BaseProperty
        {
        }
    }
}
