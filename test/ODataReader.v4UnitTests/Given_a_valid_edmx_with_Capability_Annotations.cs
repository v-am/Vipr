﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Recipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Vipr.Reader.OData.v4;
using Vipr.Core;
using Vipr.Core.CodeModel;
using Vipr.Core.CodeModel.Vocabularies.Capabilities;
using Xunit;

namespace ODataReader.v4UnitTests
{
    public class Given_a_valid_edmx_with_Capability_Annotations
    {
        [Fact]
        public void When_EntitySet_has_no_Capability_Annotation_Then_Its_OdcmProperty_has_all_the_default_OdcmCapabilities()
        {
            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.RandomElement();
            OdcmType odcmEntityType = odcmEntitySet.Projection.Type;

            odcmEntitySet.Projection.Capabilities
                .Should()
                .BeEmpty(
                "because an entity set without any capability annotation should have default capabilities");

            VerifyTypeProjections(odcmEntitySet);

            odcmEntityType.DefaultProjection.Capabilities
                .Should()
                .BeEmpty(
                    "because every OdcmType should have a default Projection with default capabilities");
        }

        [Fact]
        public void When_Singleton_has_no_Capability_Annotation_Then_Its_OdcmProperty_has_all_the_default_OdcmCapabilities()
        {
            var entityTypeElement = _entityTypeElements.RandomElement();
            var singletonElement = Any.Csdl.Singleton();
            var singletonName = singletonElement.GetAttribute("Name");
            singletonElement.AddAttribute("Type",
                string.Format("{0}.{1}", _schemaNamespace, entityTypeElement.GetAttribute("Name")));
            _entityContainerElement.Add(singletonElement);

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmSingleton = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == singletonName);
            OdcmType odcmEntityType = odcmSingleton.Projection.Type;

            odcmSingleton.Projection.Capabilities
                .Should()
                .BeEmpty(
                "because a singleton without any capability annotation should have default capabilities");

            VerifyTypeProjections(odcmSingleton);

            odcmEntityType.DefaultProjection.Capabilities
                .Should()
                .BeEmpty(
                    "because every OdcmType should have a default Projection with default capabilities");
        }

        [Fact]
        public void When_NavigationProperty_has_no_Capability_Annotation_Then_Its_OdcmProperty_has_all_the_default_OdcmCapabilities()
        {
            var insertable = Any.Bool();
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            //only entityset has insert restriction, the navigation properties are not annotated
            entitySetElement.Add(Any.Csdl.InsertRestrictionAnnotation(insertable, null));

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            OdcmProperty odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);
            OdcmType odcmEntityType = odcmEntitySet.Projection.Type;
            // TODO: Here we only look at navigation properties (not at just properties)
            OdcmProperty odcmNavProperty = (odcmEntityType as OdcmClass).Properties.Where(x => x.IsLink).RandomElement();

            odcmNavProperty.Projection.Capabilities
                .Should()
                .BeEmpty(
                "because a navigation property without any capability annotation should have default capabilities");

            VerifyTypeProjections(odcmNavProperty);

            odcmEntityType.DefaultProjection.Capabilities
                .Should()
                .BeEmpty(
                    "because every OdcmType should have a default Projection with default capabilities");
        }

        [Fact]
        public void When_EntitySet_has_no_specific_Capability_Annotation_and_no_default_is_provided_Then_its_Projection_has_no_value_for_this_Capability()
        {
            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.RandomElement();

            odcmEntitySet.BooleanValueOf(Any.Word())
                .Should()
                .Be(null, "because an entity set without this capability annotation shouldn't have any specific value");

            odcmEntitySet.EnumValueOf(Any.Word())
                .Should()
                .BeNull("because an entity set without this capability annotation shouldn't have any specific value");

            odcmEntitySet.StringCollectionValueOf(Any.Word())
                .Should()
                .BeNull("because an entity set without this capability annotation shouldn't have any specific value");
        }

        [Fact]
        public void When_EntitySet_has_no_specific_Capability_Annotation_and_default_is_provided_Then_its_Projection_has_default_value_for_this_Capability()
        {
            bool value = true;
            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.RandomElement();

            var randomTerm = Any.Word();
            var randomTerm2 = Any.Word();

            OdcmProjection.UserDefaultCapabilityProvider = (odcmObject, term) =>
            {
                if (term == randomTerm)
                {
                    return new OdcmBooleanCapability(value, term);
                }
                else if (term == randomTerm2 && odcmObject is OdcmServiceClass)
                {
                    return new OdcmBooleanCapability(!value, term);
                }
                return null;
            };

            odcmEntitySet.BooleanValueOf(randomTerm)
                .Should()
                .Be(value, "because an entity set without this capability annotation should have specified default value");

            odcmEntitySet.BooleanValueOf(randomTerm2)
                .Should()
                .Be(null, "because an entity set without this capability annotation should have specified default value");

            odcmEntityContainer.BooleanValueOf(randomTerm2)
                .Should()
                .Be(!value, "because an entity set without this capability annotation should have specified default value");
        }

        [Fact]
        public void When_NavigationProperty_has_InsertRestriction_Then_its_OdcmProperty_has_OdcmInsertCapability()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entityTypeElement = GetRandomEntityTypeElement();
                var navPropertyElement = GetRandomNavigationPropertyElement(entityTypeElement);

                navPropertyElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(insertable));

                var odcmEntityType = GetOdcmEntityType(GetOdcmModel(), entityTypeElement.GetName());

                var odcmNavigationProperty = GetOdcmProperty(odcmEntityType, navPropertyElement.GetName());

                odcmNavigationProperty.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because a navigation property with insert annotation should have OdcmInsertCapability");

                odcmNavigationProperty.BooleanValueOf("Insertable")
                    .Should()
                    .Be(insertable, "Because a navigation property with insert annotation should have OdcmInsertCapability");

                VerifyTypeProjections(odcmNavigationProperty);
            }
        }

        [Fact]
        public void When_Property_has_InsertRestriction_Then_its_OdcmProperty_has_OdcmInsertCapability()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entityTypeElement = GetRandomEntityTypeElement();

                var propertyElement = Any.Csdl.Property("Collection(Edm.String)");
                propertyElement.Add(Any.Csdl.InsertRestrictionAnnotation(insertable));
                entityTypeElement.Add(propertyElement);

                var odcmModel = GetOdcmModel();

                var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());

                var odcmProperty = (odcmEntityType as OdcmEntityClass)
                                        .Properties
                                        .Single(x => x.Name == propertyElement.GetName());

                odcmProperty.Projection.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because an entity set with insert annotation should have OdcmInsertCapability");
            }
        }

        [Fact]
        public void When_EntityType_has_InsertRestriction_Then_Referring_EntitySet_has_same_InsertRestriction()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entitySetElement = GetRandomEntitySetElement();
                var entityTypeElement = EntityTypeElementFromEntitySetElement(entitySetElement);

                entityTypeElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(insertable));

                var odcmEntitySet = GetOdcmEntitySet(GetOdcmModel(), entitySetElement.GetName());

                odcmEntitySet.Projection.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because an entity set with insert annotation should have OdcmInsertCapability");

                VerifyTypeProjections(odcmEntitySet);
            }
        }

        [Fact]
        public void When_EntityType_has_TopSupportedCapability_Then_Referring_OdcmEntitySet_has_same_Capability()
        {
            string term = "Org.OData.Capabilities.V1.TopSupported";

            foreach (bool topSupported in FalseTrue())
            {
                var entitySetElement = GetRandomEntitySetElement();
                var entityTypeElement = EntityTypeElementFromEntitySetElement(entitySetElement);

                entityTypeElement.SetAnnotation(Any.Csdl.BooleanCapabilityAnnotation(topSupported, term));

                var odcmEntitySet = GetOdcmEntitySet(GetOdcmModel(), entitySetElement.GetName());

                odcmEntitySet.Supports(term)
                    .Should()
                    .Be(topSupported, "Because an entity set with annotated entity type should inherit this annotation");

                VerifyTypeProjections(odcmEntitySet);
            }
        }

        [Fact]
        public void When_EntitySet_has_TopSupportedCapability_Then_OdcmEntitySet_has_same_Capability()
        {
            string term = "Org.OData.Capabilities.V1.TopSupported";

            foreach (bool topSupported in FalseTrue())
            {
                var entitySetElement = GetRandomEntitySetElement();

                entitySetElement.SetAnnotation(Any.Csdl.BooleanCapabilityAnnotation(topSupported, term));

                var odcmEntitySet = GetOdcmEntitySet(GetOdcmModel(), entitySetElement.GetName());

                odcmEntitySet.Supports(term)
                    .Should()
                    .Be(topSupported, "Because an entity set with this annotation should have matching capability");

                VerifyTypeProjections(odcmEntitySet);
            }
        }

        [Fact]
        public void When_EntityType_has_InsertRestriction_Then_Referring_NavigationProperty_has_same_InsertRestriction()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entityTypeElement = GetRandomEntityTypeElement();

                entityTypeElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(insertable));

                var odcmNavigationProperty = GetRandomOdcmPropertyForEntityType(GetOdcmModel(), entityTypeElement.GetName());

                odcmNavigationProperty.Projection.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because navigation property referring to EntityType with insert annotation should have OdcmInsertCapability");

                VerifyTypeProjections(odcmNavigationProperty);
            }
        }

        [Fact]
        public void When_both_EntityType_and_referring_EntitySet_have_InsertRestriction_Then_EntityType_restriction_is_overridden()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entitySetElement = GetRandomEntitySetElement();
                var entityTypeElement = EntityTypeElementFromEntitySetElement(entitySetElement);

                entitySetElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(insertable));
                entityTypeElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(!insertable));

                var odcmEntitySet = GetOdcmEntitySet(GetOdcmModel(), entitySetElement.GetName());

                odcmEntitySet.Projection.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because an entity set with explicit insert annotation should override EntityType insert annotation");

                VerifyTypeProjections(odcmEntitySet);
            }
        }

        [Fact]
        public void When_both_EntityType_and_referring_NavigationProperty_have_InsertRestriction_Then_EntityType_restriction_is_overridden()
        {
            foreach (bool insertable in FalseTrue())
            {
                var entityTypeElement = GetRandomEntityTypeElement();
                entityTypeElement.Add(Any.Csdl.InsertRestrictionAnnotation(!insertable));

                var navPropertyElement = _edmxElement
                            .Descendants()
                            .Where(element => element.Name.LocalName == "NavigationProperty")
                            .Where(e => e.GetAttribute("Type").EndsWith(entityTypeElement.GetName()) ||
                                        e.GetAttribute("Type").EndsWith(entityTypeElement.GetName() + ")"))
                            .RandomElement();

                navPropertyElement.SetAnnotation(Any.Csdl.InsertRestrictionAnnotation(insertable));

                var odcmModel = GetOdcmModel();

                var odcmParentEntityType = GetOdcmEntityType(odcmModel, navPropertyElement.Parent.GetName());
                var odcmNavigationProperty = GetOdcmProperty(odcmParentEntityType, navPropertyElement.GetName());

                odcmNavigationProperty.Projection.SupportsInsert()
                    .Should()
                    .Be(insertable, "Because navigation property referring to EntityType with insert annotation should have OdcmInsertCapability");

                VerifyTypeProjections(odcmNavigationProperty);
            }
        }

        [Fact]
        public void When_EntityContainer_has_boolean_Capability_Then_it_has_Matching_Projection()
        {
            string term = "Org.OData.Capabilities.V1.BatchSupported";

            foreach (bool value in FalseTrue())
            {
                _entityContainerElement.SetAnnotation(Any.Csdl.BooleanCapabilityAnnotation(value, term));

                var odcmModel = GetOdcmModel();

                var odcmEntityContainer = GetOdcmEntityContainer(odcmModel) as OdcmServiceClass;

                odcmEntityContainer.Projection.Supports(term)
                    .Should()
                    .Be(value, "Because EntityContainer should have the same capability");
            }
        }

        [Fact]
        public void When_EntityContainer_has_string_collection_Capability_Then_it_has_Matching_Projection()
        {
            string term = "Org.OData.Capabilities.V1.AcceptableEncodings";

            var inputValues = new List<string> { "gzip", "zip"};
            _entityContainerElement.SetAnnotation(Any.Csdl.StringListCapabilityAnnotation(inputValues, term));

            var odcmModel = GetOdcmModel();

            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel) as OdcmServiceClass;

            var values = odcmEntityContainer.StringCollectionValueOf(term);

            values.Count().Should()
                .Be(2, "Because we specified that many collection items");

            values.Should()
                .BeEquivalentTo(inputValues, "Because EntityContainer should have appropriate capability");
        }

        [Fact]
        public void When_EntityContainer_is_annotated_with_CallbackSupported_Then_Its_OdcmProperty_has_Matching_Projection()
        {
            string term = "Org.OData.Capabilities.V1.CallbackSupported";

            _entityContainerElement.SetAnnotation(Any.Csdl.CallbackSupportedAnnotation(count: 2));

            var odcmModel = GetOdcmModel();

            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel) as OdcmServiceClass;

            var record = odcmEntityContainer.Projection.RecordValueOf(term);

            int count = record.CallbackProtocols.Count;
            count.Should().Be(2);

            IEnumerable<dynamic> protocols = record.CallbackProtocols;
            IList<dynamic> protocols2 = protocols.ToList();

            // Verify existence of all properties
            string id = protocols.First().Id;
            string UrlTemplate = protocols.First().UrlTemplate;
            string DocumentationUrl = protocols.First().DocumentationUrl;

            string id2 = protocols2[1].Id;
            string UrlTemplate2 = protocols2[1].UrlTemplate;
            string DocumentationUrl2 = protocols2[1].DocumentationUrl;
        }

        [Fact]
        public void When_EntityContainer_is_annotated_with_arbitrary_record_Then_Its_OdcmProperty_has_Matching_Projection()
        {
            string term = $"Org.OData.Capabilities.V1.{Any.Word()}";

            string property = Any.Word();
            string property2 = Any.Word();

            _entityContainerElement.SetAnnotation(Any.Csdl.RecordAnnotation(term, property, property2));

            var odcmModel = GetOdcmModel();

            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel) as OdcmServiceClass;

            odcmEntityContainer.Projection.StringValueOf(property).Should().NotBeNull();
            odcmEntityContainer.Projection.StringValueOf(property2).Should().NotBeNull();
        }

        [Fact]
        public void When_EntityContainer_is_annotated_with_collection_of_arbitrary_records_Then_Its_OdcmProperty_has_Matching_Projection()
        {
            string term = $"Org.OData.Capabilities.V1.{Any.Word()}";

            string property = Any.Word();
            string property2 = Any.Word();

            _entityContainerElement.SetAnnotation(Any.Csdl.RecordCollectionAnnotation(term, 2, new string[] { property, property2 }));

            var odcmModel = GetOdcmModel();

            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel) as OdcmServiceClass;

            var recordList = odcmEntityContainer.Projection.CollectionValueOf(term).ToList();

            recordList.Count.Should().Be(2);

            // Verify presence of the properties
            var record = recordList[0] as IDictionary<string, object>;

            record.ContainsKey(property).Should().BeTrue();
            record.ContainsKey(property2).Should().BeTrue();
        }

        [Fact]
        public void When_EntitySet_is_annotated_with_FilterRestrictions_Then_Its_OdcmProperty_has_matching_Capabilities()
        {
            foreach (bool value in FalseTrue())
            {
                var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
                var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
                var propertyName = GetRandomProperty(entityTypeElement);

                entitySetElement.SetAnnotation(Any.Csdl.FilterRestrictionAnnotation(value, new List<string> { propertyName }));

                var odcmModel = GetOdcmModel(_edmxElement);

                var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());
                var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());
                var odcmProperty = GetOdcmProperty(odcmEntityType, propertyName);

                odcmEntitySet.BooleanValueOf("Filterable")
                    .Should()
                    .Be(value, "Because entity set should support the specified capability");

                odcmEntitySet.BooleanValueOf("RequiresFilter")
                    .Should()
                    .Be(value, "Because entity set should support the specified capability");

                odcmProperty.IsOneOf("RequiredProperties")
                    .Should()
                    .BeTrue("Because a navigation property should belong to the specified restriction collection");

                odcmProperty.IsOneOf("NonFilterableProperties")
                    .Should()
                    .BeTrue("Because a navigation property should belong to the specified restriction collection");
            }
        }

        [Fact]
        public void When_EntitySet_has_ChangeTracking_Then_Its_OdcmProperty_has_appropriate_capability()
        {
            foreach (bool value in FalseTrue())
            {
                var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
                var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
                var propertyName = GetRandomProperty(entityTypeElement);

                entitySetElement.SetAnnotation(Any.Csdl.ChangeTrackingAnnotation(value, new List<string> { propertyName }));

                var odcmModel = GetOdcmModel(_edmxElement);

                var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());
                var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());
                var odcmProperty = GetOdcmProperty(odcmEntityType, propertyName);

                odcmEntitySet.BooleanValueOf("Supported")
                    .Should()
                    .Be(value, "Because entity set should support the specified capability");

                odcmProperty.IsOneOf("FilterableProperties")
                    .Should()
                    .BeTrue("Because a navigation property should belong to the specified restriction collection");

                odcmProperty.IsOneOf("ExpandableProperties")
                    .Should()
                    .BeTrue("Because a navigation property should belong to the specified restriction collection");
            }
        }

        [Fact]
        public void When_EntitySet_has_CountRestrictions_Then_Its_OdcmProperty_has_appropriate_capability()
        {
            foreach (bool value in FalseTrue())
            {
                var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
                var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
                var propertyName = GetRandomProperty(entityTypeElement);
                var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

                entitySetElement.SetAnnotation(Any.Csdl.CountRestrictionAnnotation(value, new List<string> { propertyName }, new List<string> { navPropertyName }));

                var odcmModel = GetOdcmModel(_edmxElement);

                var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());
                var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());
                var odcmProperty = GetOdcmProperty(odcmEntityType, propertyName);
                var odcmNavProperty = GetOdcmProperty(odcmEntityType, navPropertyName);

                odcmEntitySet.BooleanValueOf("Countable")
                    .Should()
                    .Be(value, "Because entity set should support the specified capability");

                odcmProperty.IsOneOf("NonCountableProperties")
                    .Should()
                    .BeTrue("Because property should belong to the specified restriction collection");

                odcmNavProperty.IsOneOf("NonCountableNavigationProperties")
                    .Should()
                    .BeTrue("Because navigation property should belong to the specified restriction collection");
            }
        }

        [Fact]
        public void When_EntityType_has_CountRestrictions_Then_Its_OdcmEntitySet_has_appropriate_capability()
        {
            foreach (bool value in FalseTrue())
            {
                var entitySetElement = GetRandomEntitySetElement();
                var entityTypeElement = EntityTypeElementFromEntitySetElement(entitySetElement);

                var propertyName = GetRandomProperty(entityTypeElement);
                var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

                entityTypeElement.SetAnnotation(Any.Csdl.CountRestrictionAnnotation(value, new List<string> { propertyName }, new List<string> { navPropertyName }));

                var odcmModel = GetOdcmModel(_edmxElement);

                var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());
                //var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());
                //var odcmProperty = GetOdcmProperty(odcmEntityType, propertyName);
                //var odcmNavProperty = GetOdcmProperty(odcmEntityType, navPropertyName);

                odcmEntitySet.BooleanValueOf("Countable")
                    .Should()
                    .Be(value, "Because entity set should support the specified capability");

                //odcmProperty.IsOneOf("NonCountableProperties")
                //    .Should()
                //    .BeTrue("Because property should belong to the specified restriction collection");

                //odcmNavProperty.IsOneOf("NonCountableNavigationProperties")
                //    .Should()
                //    .BeTrue("Because navigation property should belong to the specified restriction collection");
            }
        }

        [Fact]
        public void When_EntitySet_has_simple_NavigationRestriction_Then_Its_OdcmProperty_has_appropriate_capability()
        {
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();

            string enumValue = "single";

            entitySetElement.Add(Any.Csdl.NavigationRestrictionAnnotation(enumValue));

            var odcmModel = GetOdcmModel(_edmxElement);

            var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());

            var value = odcmEntitySet.EnumValueOf("Navigability");

            value.First()
                .Should()
                .Be(enumValue, "Because entity set should support the specified capability");
        }

        [Fact]
        public void When_EntitySet_has_NavigationRestriction_Then_Its_OdcmProperty_has_appropriate_capability()
        {
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];

            string enumValue = "single";
            string enumValue2 = "none";
            var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

            var navigationTypes = new List<Tuple<string, string>>
            {
                Tuple.Create(navPropertyName, enumValue2)
            };

            entitySetElement.Add(Any.Csdl.NavigationRestrictionAnnotation(enumValue, navigationTypes));

            var odcmModel = GetOdcmModel(_edmxElement);

            var odcmEntitySet = GetOdcmEntitySet(odcmModel, entitySetElement.GetName());
            var odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElement.GetName());
            var odcmNavProperty = GetOdcmProperty(odcmEntityType, navPropertyName);

            odcmEntitySet.EnumValueOf("Navigability").First()
                .Should()
                .Be(enumValue, "Because entity set should support specified capability");

            odcmNavProperty.EnumValueOf("Navigability").First()
                .Should()
                .Be(enumValue2, "Because navigation property should have specified navigability");
        }

        [Fact]
        public void When_EntitySet_has_InsertRestriction_Then_Its_OdcmProperty_has_OdcmInsertCapability()
        {
            var insertable = Any.Bool();
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            entitySetElement.Add(Any.Csdl.InsertRestrictionAnnotation(insertable, null));

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);

            odcmEntitySet.Projection.SupportsInsert()
                .Should()
                .Be(insertable, "Because an entity set with insert annotation should have OdcmInsertCapability");

            VerifyTypeProjections(odcmEntitySet);
        }

        [Fact]
        public void When_EntitySet_has_UpdateRestriction_Then_Its_OdcmProperty_has_OdcmUpdateCapability()
        {
            var updatable = Any.Bool();
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            entitySetElement.Add(Any.Csdl.UpdateRestrictionAnnotation(updatable, null));

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);

            odcmEntitySet.Projection.SupportsUpdate()
                .Should()
                .Be(updatable, "Because an entity set with update annotation should have OdcmUpdateCapability");

            VerifyTypeProjections(odcmEntitySet);
        }

        [Fact]
        public void When_EntitySet_has_DeleteRestriction_Then_Its_OdcmProperty_has_OdcmDeleteCapability()
        {
            var deletable = Any.Bool();
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            entitySetElement.Add(Any.Csdl.DeleteRestrictionAnnotation(deletable, null));

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);

            odcmEntitySet.Projection.SupportsDelete()
                .Should()
                .Be(deletable, "Because an entity set with delete annotation should have OdcmDeleteCapability");

            VerifyTypeProjections(odcmEntitySet);
        }

        [Fact]
        public void When_EntitySet_has_ExpandRestriction_Then_Its_OdcmProperty_has_OdcmExpandCapability()
        {
            var expandable = Any.Bool();
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            entitySetElement.Add(Any.Csdl.ExpandRestrictionAnnotation(expandable, null));

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);

            odcmEntitySet.Projection.SupportsExpand()
                .Should()
                .Be(expandable, "Because an entity set with expand annotation should have OdcmExpandCapability");

            VerifyTypeProjections(odcmEntitySet);
        }

        [Fact]
        public void When_NavigationProperty_is_annotated_with_NonUpdatableNavigationProperties_Then_Its_OdcmProperty_has_OdcmUpdateLinkCapability()
        {
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
            var entityTypeElementName = entityTypeElement.GetAttribute("Name");
            var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

            var updatable = Any.Bool();
            entitySetElement.Add(Any.Csdl.UpdateRestrictionAnnotation(updatable, new List<string> { navPropertyName }));

            var odcmModel = GetOdcmModel(_edmxElement);
            OdcmType odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElementName);
            OdcmProperty odcmNavProperty = (odcmEntityType as OdcmClass).Properties.Single(p => p.Name == navPropertyName);

            odcmNavProperty.SupportsUpdateLink()
                .Should()
                .BeFalse("Because a navigation property with update annotation should not support OdcmUpdateLinkCapability");

            odcmNavProperty.BooleanValueOf("NonUpdatableNavigationProperties")
                .Should()
                .BeFalse("Because a navigation property with update annotation should not support OdcmUpdateLinkCapability");

            odcmNavProperty.IsOneOf("NonUpdatableNavigationProperties")
                .Should()
                .BeTrue("Because a navigation property with update annotation should not support OdcmUpdateLinkCapability");

            VerifyTypeProjections(odcmNavProperty);
        }

        [Fact]
        public void When_NavigationProperty_is_annotated_with_NonDeletableNavigationProperties_Then_Its_OdcmProperty_has_OdcmDeleteLinkCapability()
        {
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
            var entityTypeElementName = entityTypeElement.GetAttribute("Name");
            var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

            var deletable = Any.Bool();
            entitySetElement.Add(Any.Csdl.DeleteRestrictionAnnotation(deletable, new List<string> { navPropertyName }));

            var odcmModel = GetOdcmModel(_edmxElement);
            OdcmType odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElementName);
            OdcmProperty odcmNavProperty = (odcmEntityType as OdcmClass).Properties.Single(p => p.Name == navPropertyName);

            odcmNavProperty.Projection.SupportsDeleteLink()
                .Should()
                .BeFalse("Because a navigation property with delete annotation should not support OdcmDeleteCapability");

            VerifyTypeProjections(odcmNavProperty);
        }

        [Fact]
        public void When_EntitySet_has_multiple_Capability_Annotations_Then_Its_OdcmProperty_has_corresponding_OdcmCapabilities()
        {
            var booleanValue = Any.Bool();
            var randomRestrictionAnnotations = GetRandomRestrictionAnnotationElements(booleanValue, null);

            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entitySetElementName = entitySetElement.GetAttribute("Name");
            entitySetElement.Add(randomRestrictionAnnotations);

            var odcmModel = GetOdcmModel(_edmxElement);
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            var odcmEntitySet = odcmEntityContainer.As<OdcmClass>().Properties.Single(p => p.Name == entitySetElementName);

            foreach (var restrictionAnnotation in randomRestrictionAnnotations)
            {
                var termName = GetAnnotationTermName(restrictionAnnotation);
                HasOdcmCapability(odcmEntitySet, termName, booleanValue)
                    .Should()
                    .BeTrue("Because an entity set should have the correct OdcmCapability");
            }

            VerifyTypeProjections(odcmEntitySet);
        }

        [Fact]
        public void When_NavigationProperty_has_multiple_Capability_Annotations_Then_Its_OdcmProperty_has_corresponding_OdcmCapabilities()
        {
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
            var entityTypeElementName = entityTypeElement.GetAttribute("Name");
            var navPropertyName = GetRandomNavigationProperty(entityTypeElement);

            entitySetElement.Add(Any.Csdl.UpdateRestrictionAnnotation(Any.Bool(),
                new List<string> { navPropertyName }));
            entitySetElement.Add(Any.Csdl.DeleteRestrictionAnnotation(Any.Bool(),
                new List<string> { navPropertyName }));

            var odcmModel = GetOdcmModel(_edmxElement);
            OdcmType odcmEntityType = GetOdcmEntityType(odcmModel, entityTypeElementName);
            OdcmProperty odcmNavProperty = (odcmEntityType as OdcmClass).Properties.Single(p => p.Name == navPropertyName);

            odcmNavProperty.Projection.SupportsUpdateLink()
                    .Should()
                    .BeFalse("Because a navigation property should have the correct OdcmUpdateLinkCapability");

            odcmNavProperty.Projection.SupportsDeleteLink()
                    .Should()
                    .BeFalse("Because a navigation property should have the correct OdcmDeleteLinkCapability");

            VerifyTypeProjections(odcmNavProperty);
        }

        [Fact]
        public void When_multiple_OdcmProperties_with_same_OdcmType_have_different_Capabilities_Then_they_have_two_different_OdcmProjections()
        {
            // get a random entity set
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            // get the corresponding entity type
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
            var entityTypeElementName = entityTypeElement.GetAttribute("Name");
            // get a random entity type which is different from the above entity type
            // we will use this as the type for a navigation property
            var navigationPropertyTypeName =
                _entitySetToEntityTypeMapping.Values.Where(element => element != entityTypeElement)
                    .RandomElement()
                    .GetAttribute("Name");

            // Lets create two navigation properties of the same type
            var navPropertyElement1 = Any.Csdl.NavigationProperty(OdcmObject.MakeCanonicalName(navigationPropertyTypeName, _schemaNamespace));
            var navPropertyElement1Name = navPropertyElement1.GetAttribute("Name");
            var navPropertyElement2 = Any.Csdl.NavigationProperty(OdcmObject.MakeCanonicalName(navigationPropertyTypeName, _schemaNamespace));
            var navPropertyElement2Name = navPropertyElement2.GetAttribute("Name");

            // now add these two navigation properties to an EntityType
            entityTypeElement = _schema.Elements().Single(e => e.GetAttribute("Name") == entityTypeElement.GetAttribute("Name"));
            entityTypeElement.Add(navPropertyElement1);
            entityTypeElement.Add(navPropertyElement2);

            // Scenario where they share one restriction annotation, but have two other different restriction annotation
            // This must create two different projections for these navigation properties
            entitySetElement.Add(Any.Csdl.InsertRestrictionAnnotation(Any.Bool(),
                new List<string> { navPropertyElement1Name, navPropertyElement2Name }));
            entitySetElement.Add(Any.Csdl.UpdateRestrictionAnnotation(Any.Bool(), new List<string> { navPropertyElement1Name }));
            entitySetElement.Add(Any.Csdl.DeleteRestrictionAnnotation(Any.Bool(), new List<string> { navPropertyElement2Name }));

            var odcmModel = GetOdcmModel(_edmxElement);
            OdcmType odcmType = GetOdcmEntityType(odcmModel, entityTypeElementName);

            OdcmProperty odcmNavProperty1 = (odcmType as OdcmClass).Properties.Single(p => p.Name == navPropertyElement1Name);
            OdcmProperty odcmNavProperty2 = (odcmType as OdcmClass).Properties.Single(p => p.Name == navPropertyElement2Name);

            odcmNavProperty1.Projection
                .Should()
                .NotBeSameAs(odcmNavProperty2.Projection,
                    "Two navigation properties with different capabilities should have different Projections");

            VerifyTypeProjections(odcmNavProperty1);
            VerifyTypeProjections(odcmNavProperty2);
        }

        [Fact]
        public void When_multiple_OdcmProperties_with_same_OdcmType_have_same_Capabilities_Then_they_have_one_shared_OdcmProjection()
        {
            // get a random entity set
            var entitySetElement = _entitySetToEntityTypeMapping.Keys.RandomElement();
            // get the corresponding entity type
            var entityTypeElement = _entitySetToEntityTypeMapping[entitySetElement];
            var entityTypeElementName = entityTypeElement.GetAttribute("Name");
            // get a random entity type which is different from the above entity type
            // we will use this as the type for a navigation property
            var navigationPropertyTypeName =
                _entitySetToEntityTypeMapping.Values.Where(element => element != entityTypeElement)
                    .RandomElement()
                    .GetAttribute("Name");

            // Lets create two navigation properties of the same type
            var navPropertyElement1 = Any.Csdl.NavigationProperty(OdcmObject.MakeCanonicalName(navigationPropertyTypeName, _schemaNamespace));
            var navPropertyElement1Name = navPropertyElement1.GetAttribute("Name");
            var navPropertyElement2 = Any.Csdl.NavigationProperty(OdcmObject.MakeCanonicalName(navigationPropertyTypeName, _schemaNamespace));
            var navPropertyElement2Name = navPropertyElement2.GetAttribute("Name");

            // now add these two navigation properties to an EntityType
            entityTypeElement = _schema.Elements().Single(e => e.GetAttribute("Name") == entityTypeElement.GetAttribute("Name"));
            entityTypeElement.Add(navPropertyElement1);
            entityTypeElement.Add(navPropertyElement2);

            // make sure that these two navigation properites have same restriction annotation
            entitySetElement.Add(Any.Csdl.UpdateRestrictionAnnotation(Any.Bool(),
                new List<string> {navPropertyElement1Name, navPropertyElement2Name}));
            entitySetElement.Add(Any.Csdl.DeleteRestrictionAnnotation(Any.Bool(),
                new List<string> {navPropertyElement1Name, navPropertyElement2Name}));

            var odcmModel = GetOdcmModel(_edmxElement);
            OdcmType odcmType = GetOdcmEntityType(odcmModel, entityTypeElementName);
            OdcmType odcmNavPropertyType = GetOdcmEntityType(odcmModel, navigationPropertyTypeName);

            odcmNavPropertyType.Projections.Count()
                .Should()
                .Be(2, "Because the OdcmModel must create 2 projections including the Projection with Default Capabilities");

            OdcmProperty odcmNavProperty1 = (odcmType as OdcmClass).Properties.Single(p => p.Name == navPropertyElement1Name);
            OdcmProperty odcmNavProperty2 = (odcmType as OdcmClass).Properties.Single(p => p.Name == navPropertyElement2Name);

            odcmNavProperty1.Projection.Key.Should().BeEquivalentTo(odcmNavProperty2.Projection.Key);

            VerifyTypeProjections(odcmNavProperty1);
        }

        public Given_a_valid_edmx_with_Capability_Annotations()
        {
            _odcmReader = new OdcmReader();
            _entitySetToEntityTypeMapping = new Dictionary<XElement, XElement>();

            _edmxElement = Any.Csdl.EdmxToSchema(schema => _schema = schema);
            _schemaNamespace = _schema.Attribute("Namespace").Value;

            _entityContainerElement = Any.Csdl.EntityContainer();
            _entityContainerName = _entityContainerElement.Attribute("Name").Value;

            string entityTypesEdmx = string.Format(ENTITY_TYPES_EDMX, _schemaNamespace);
            _entityTypeElements = XElement.Parse(entityTypesEdmx)
                                        .Elements()
                                        .Where(element => element.Name.LocalName == "EntityType");

            foreach (var entityTypeElement in _entityTypeElements)
            {
                _schema.Add(entityTypeElement);
                var entitySetElement = Any.Csdl.EntitySet();
                entitySetElement.AddAttribute("EntityType",
                    string.Format("{0}.{1}", _schemaNamespace, entityTypeElement.GetName()));
                _entitySetToEntityTypeMapping.Add(entitySetElement, entityTypeElement);
                _entityContainerElement.Add(entitySetElement);
            }

            _schema.Add(_entityContainerElement);
        }

        private Vipr.Reader.OData.v4.OdcmReader _odcmReader;
        private string _schemaNamespace;
        private string _entityContainerName;
        private XElement _edmxElement;
        private XElement _schema;
        private XElement _entityContainerElement;
        private Dictionary<XElement, XElement> _entitySetToEntityTypeMapping;
        private IEnumerable<XElement> _entityTypeElements;


        private const string ENTITY_TYPES_EDMX =
                      @"<EntityTypes>
                          <EntityType Name=""Notebook"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">	
                            <Property Name=""LastModifiedDateTime"" Type=""Edm.DateTimeOffset""/>
                            <Property Name=""Name"" Type=""Edm.String""/>
	                        <NavigationProperty Name=""sections"" Type=""Collection({0}.Section)"" />
	                        <NavigationProperty Name=""sectionGroups"" Type=""Collection({0}.SectionGroup)"" />
                        </EntityType>
                          <EntityType Name=""SectionGroup"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
                            <Property Name=""LastModifiedDateTime"" Type=""Edm.DateTimeOffset""/>
                            <Property Name=""Name"" Type=""Edm.String""/>
	                        <NavigationProperty Name=""parentNotebook"" Type=""{0}.Notebook"" />
	                        <NavigationProperty Name=""parentSectionGroup"" Type=""{0}.SectionGroup"" />
	                        <NavigationProperty Name=""sections"" Type=""Collection({0}.Section)"" />
	                        <NavigationProperty Name=""sectionGroups"" Type=""Collection({0}.SectionGroup)"" />
                          </EntityType>
                          <EntityType Name=""Section"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
                            <Property Name=""LastModifiedDateTime"" Type=""Edm.DateTimeOffset""/>
                            <Property Name=""Name"" Type=""Edm.String""/>
	                        <NavigationProperty Name=""parentNotebook"" Type=""{0}.Notebook"" />
	                        <NavigationProperty Name=""parentSectionGroup"" Type=""{0}.SectionGroup"" />
	                        <NavigationProperty Name=""pages"" Type=""Collection({0}.Page)"" />
                          </EntityType>
                          <EntityType Name=""Page"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
                            <Property Name=""LastModifiedDateTime"" Type=""Edm.DateTimeOffset""/>
                            <Property Name=""Name"" Type=""Edm.String""/>
	                        <NavigationProperty Name=""parentSection"" Type=""{0}.Section"" />
	                        <NavigationProperty Name=""parentNotebook"" Type=""{0}.Notebook"" />
                          </EntityType>
                        </EntityTypes>";

        private OdcmModel GetOdcmModel(XElement edmxElement)
        {
            var serviceMetadata = new List<TextFile>
            {
                new TextFile("$metadata", edmxElement.ToString())
            };

            var odcmModel = _odcmReader.GenerateOdcmModel(serviceMetadata);
            return odcmModel;
        }

        private OdcmModel GetOdcmModel()
        {
            return GetOdcmModel(_edmxElement);
        }

        private OdcmType GetOdcmEntityContainer(OdcmModel odcmModel)
        {
            OdcmType odcmEntityContainer;
            odcmModel.TryResolveType(_entityContainerName, _schemaNamespace, out odcmEntityContainer)
                .Should()
                .BeTrue("because an entity container in the schema should result in an OdcmType");

            return odcmEntityContainer;
        }

        private OdcmType GetOdcmEntityType(OdcmModel odcmModel, string entityTypeName)
        {
            OdcmType odcmType;
            odcmModel.TryResolveType(entityTypeName, _schemaNamespace, out odcmType)
                .Should()
                .BeTrue("because an entity type in the schema should result in an OdcmType");

            return odcmType;
        }

        private string GetRandomProperty(XElement entityTypeElement)
        {
            string name = string.Empty;
            XElement element = GetRandomElementByLocalName(entityTypeElement, "Property");

            if (element != null)
            {
                name = element.GetAttribute("Name");
            }

            return name;
        }

        private string GetRandomNavigationProperty(XElement entityTypeElement)
        {
            string navPropertyName = string.Empty;
            XElement navPropertyElement = GetRandomElementByLocalName(entityTypeElement, "NavigationProperty");

            if (navPropertyElement != null)
            {
                navPropertyName = navPropertyElement.GetAttribute("Name");
            }

            return navPropertyName;
        }

        private XElement GetRandomNavigationPropertyElement(XElement entityTypeElement)
        {
#if true
            return GetRandomElementByLocalName(entityTypeElement, "NavigationProperty");
#else
            XElement navPropertyElement =
                entityTypeElement.Elements()
                    .Where(element => element.Name.LocalName == "NavigationProperty")
                    .RandomElement();

            return navPropertyElement;
#endif
        }

        private XElement GetRandomElementByLocalName(XElement entityTypeElement, string localName)
        {
            XElement element =
                entityTypeElement.Elements()
                    .Where(e => e.Name.LocalName == localName)
                    .RandomElement();

            return element;
        }

        private IEnumerable<XElement> GetRandomRestrictionAnnotationElements(bool booleanValue, List<string> navPropertyPaths)
        {
            List<XElement> restrictionAnnotations = new List<XElement>()
            {
                Any.Csdl.InsertRestrictionAnnotation(booleanValue, navPropertyPaths),
                Any.Csdl.UpdateRestrictionAnnotation(booleanValue, navPropertyPaths),
                Any.Csdl.DeleteRestrictionAnnotation(booleanValue, navPropertyPaths),
                Any.Csdl.ExpandRestrictionAnnotation(booleanValue, navPropertyPaths)
            };

            int count = Any.Int(2, restrictionAnnotations.Count);
            return restrictionAnnotations.RandomSubset(count);
        }

        private string GetAnnotationTermName(XElement annotation)
        {
            string termName = string.Empty;
            if (annotation.Name.LocalName == "Annotation")
            {
                termName = annotation.GetAttribute("Term");

                // Assume we are looking for the single [boolean] property value
                var propertyValueElement = annotation.Descendants()
                                                .First(x => x.Name.LocalName == "PropertyValue" && !x.HasElements);

                var name = propertyValueElement.GetAttribute("Property");

                termName += "/" + name;
            }

            return termName;
        }

        private bool HasOdcmCapability(OdcmProperty odcmProperty, string termName, bool booleanValue)
        {
            var odcmCapability = odcmProperty.Projection.Capabilities.SingleOrDefault(capability =>
            {
                return capability is OdcmBooleanCapability &&
                capability.TermName == termName && (capability as OdcmBooleanCapability).Value == booleanValue;
            });

            return odcmCapability != null;
        }

        private OdcmProperty GetOdcmEntitySet(OdcmModel odcmModel, string entitySetName)
        {
            var odcmEntityContainer = GetOdcmEntityContainer(odcmModel);

            return GetOdcmProperty(odcmEntityContainer, entitySetName);
        }

        private XElement GetRandomEntitySetElement()
        {
            return _entitySetToEntityTypeMapping.Keys.RandomElement();
        }

        private XElement EntityTypeElementFromEntitySetElement(XElement entitySetElement)
        {
            var entityTypeElementName = _entitySetToEntityTypeMapping[entitySetElement].GetName();

            return GetEntityTypeElement(entityTypeElementName);
        }

        private XElement GetRandomEntityTypeElement()
        {
            var entityTypeElementName = _entitySetToEntityTypeMapping
                                                .Values
                                                .RandomElement()
                                                .GetName();

            return GetEntityTypeElement(entityTypeElementName);
        }

        private XElement GetEntityTypeElement(string entityTypeElementName)
        {
            return _edmxElement
                        .Descendants()
                        .Where(element => element.Name.LocalName == "EntityType")
                        .Single(element => entityTypeElementName == element.GetName());
        }

        private IEnumerable<bool> FalseTrue()
        {
            yield return false;
            yield return true;
        }

        private OdcmProperty GetRandomOdcmPropertyForEntityType(OdcmModel odcmModel, string entityTypeName)
        {
            return odcmModel
                        .Namespaces[1]
                        .Types
                        .Where(t => t is OdcmEntityClass)
                        .Select(t => t as OdcmEntityClass)
                        .SelectMany(x => x.Properties)
                        .Where(x => x.Type.Name == entityTypeName)
                        .RandomElement();
        }

        private OdcmProperty GetOdcmProperty(OdcmType odcmType, string elementName)
        {
            return odcmType
                        .As<OdcmClass>()
                        .Properties
                        .Single(p => p.Name == elementName);
        }

        private void VerifyTypeProjections(OdcmProperty odcmProperty)
        {
            odcmProperty.Projection.Type.ProjectionKeys.Any(x => x == odcmProperty.Projection.Key)
                .Should()
                .BeTrue("because this OdcmProperty Projection is present in its OdcmType's cache of Projections");
        }
    }
}