﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Vipr.Core.CodeModel;

namespace Vipr.Writer.CSharp.Lite
{
    public class CollectionUpdateAsyncMethod : Method
    {
        public OdcmClass OdcmClass { get; private set; }

        public CollectionUpdateAsyncMethod(OdcmClass odcmClass)
        {
            Visibility = Visibility.Public;
            Name = "UpdateAsync";
            Parameters = new[]
            {
                new Parameter(new Type(NamesService.GetConcreteInterfaceName(odcmClass)), "item"),
                new Parameter(new Type(NamesService.GetPrimitiveTypeName("Boolean")), "deferSaveChanges", "false")
            };
            ReturnType = new Type(Identifier.Task);
            OdcmClass = odcmClass;
        }
    }
}