// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Vipr.Core;
using Vipr.Core.CodeModel;

namespace Vipr.Writer.CSharp
{
    public class Fields
    {
        public static IEnumerable<Field> ForGeneratedEdmModel(OdcmModel odcmModel)
        {
            return new[]
            {
                new Field("ParsedModel",
                    new Type(new Identifier("global::Microsoft.OData.Edm", "IEdmModel")),
                    "LoadModelFromString()",
                    false,
                    true),
                new ModelField("Edmx",
                    odcmModel)
            };
        }

        public static IEnumerable<Field> ForEntityContainer(OdcmClass odcmContainer)
        {
            return new[]
            {
                new Field("_path", new Type(new Identifier("System", "String")), "\"\"", true),
            }.Concat(GetFetcherFields(odcmContainer));
        }

        public static IEnumerable<Field> ForComplex(OdcmClass odcmClass)
        {
            return ForConcrete(odcmClass);
        }

        public static IEnumerable<Field> ForConcrete(OdcmClass odcmClass)
        {
            return GetNavigationFields(odcmClass)
                .Concat(GetFetcherFields(odcmClass))
                .Concat(GetStructuralFields(odcmClass))
                .Concat(GetCollectionFields(odcmClass));
        }

        public static IEnumerable<Field> ForFetcher(OdcmClass odcmClass)
        {
            return GetFetcherFields(odcmClass);
        }

        private static IEnumerable<Field> GetCollectionFields(OdcmClass odcmClass)
        {
            return odcmClass.NavigationProperties(true).Select(Field.ForConcreteNavigationCollectionProperty);
        }

        private static IEnumerable<Field> GetFetcherFields(OdcmClass odcmClass)
        {
            return odcmClass.NavigationProperties(false).Select(Field.ForNavigationFetcherProperty)
                .Concat(odcmClass.NavigationProperties(true).Select(Field.ForFetcherNavigationCollectionProperty));
        }

        private static IEnumerable<Field> GetNavigationFields(OdcmClass odcmClass)
        {
            return odcmClass.NavigationProperties(false).Select(Field.ForNavigationProperty);
        }

        private static IEnumerable<Field> GetStructuralFields(OdcmClass odcmClass)
        {
            return odcmClass.StructuralProperties().Select(Field.ForStructuralProperty);
        }

        public static IEnumerable<Field> Empty { get { return Enumerable.Empty<Field>(); } }
    }
}