﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query.PropertyPages
{
    public class UIPropertyEditorDataProducerTests
    {
        [Fact]
        public void WhenCreatingAnEntityFromAParentAndEditor_TheIdIsTheEditorType()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyEditorPropertiesAvailableStatus(includeName: false);

            var parentEntity = IEntityWithIdFactory.Create(key: "parentId", value: "aaa");
            var editor = new ValueEditor { EditorType = "My.Editor.Type" };

            var result = (UIPropertyEditorValue)UIPropertyEditorDataProducer.CreateEditorValue(parentEntity, editor, properties);

            Assert.Equal(expected: "My.Editor.Type", actual: result.Id[ProjectModelIdentityKeys.EditorName]);
        }

        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyEditorPropertiesAvailableStatus(includeName: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "EditorKey", value: "bbb");
            var editor = new ValueEditor { EditorType = "My.Editor.Type" };

            var result = (UIPropertyEditorValue)UIPropertyEditorDataProducer.CreateEditorValue(entityRuntime, id, editor, properties);

            Assert.Equal(expected: "My.Editor.Type", actual: result.Name);
        }

        [Fact]
        public void WhenAnEditorValueIsCreated_TheEditorIsTheProviderState()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyEditorPropertiesAvailableStatus(includeName: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "EditorKey", value: "bbb");
            var editor = new ValueEditor { EditorType = "My.Editor.Type" };

            var result = (UIPropertyEditorValue)UIPropertyEditorDataProducer.CreateEditorValue(entityRuntime, id, editor, properties);

            Assert.Equal(expected: editor, actual: ((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingEditorsFromAProperty_OneEntityIsReturnedPerEditor()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyEditorPropertiesAvailableStatus(includeName: true);

            var parentEntity = IEntityWithIdFactory.Create(key: "parentKey", value: "parentId");
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.Add(
                new TestProperty
                {
                    Name = "MyProperty",
                    ValueEditors =
                    {
                        new ValueEditor { EditorType = "Alpha" },
                        new ValueEditor { EditorType = "Beta" },
                        new ValueEditor { EditorType = "Gamma" }
                    }
                });
            rule.EndInit();

            var results = UIPropertyEditorDataProducer.CreateEditorValues(parentEntity, rule, "MyProperty", properties);

            Assert.Collection(results, new Action<IEntityValue>[]
            {
                entity => assertEqual(entity, expectedName: "Alpha"),
                entity => assertEqual(entity, expectedName: "Beta"),
                entity => assertEqual(entity, expectedName: "Gamma")
            });

            static void assertEqual(IEntityValue entity, string expectedName)
            {
                var editorEntity = (UIPropertyEditorValue)entity;
                Assert.Equal(expectedName, editorEntity.Name);
            }
        }

        private class TestProperty : BaseProperty
        {
        }
    }
}
