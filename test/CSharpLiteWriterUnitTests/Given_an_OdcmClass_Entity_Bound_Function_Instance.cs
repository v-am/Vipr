﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.MockService;
using Microsoft.MockService.Extensions.ODataV4;
using Microsoft.OData.ProxyExtensions.Lite;
using Vipr.Core.CodeModel;
using Xunit;

namespace CSharpLiteWriterUnitTests
{
    public class Given_an_OdcmClass_Entity_Bound_Function_Instance : Given_an_OdcmClass_Entity_Bound_Function_Base
    {
        public Given_an_OdcmClass_Entity_Bound_Function_Instance()
        {
            IsCollection = false;

            ReturnTypeGenerator = (t) => typeof(Task<>).MakeGenericType(t);

            Init();
        }

        [Fact]
        public void The_Fetcher_parses_the_response()
        {
            Init(m =>
            {
                m.Verbs = OdcmAllowedVerbs.Get;
                m.Parameters.Clear();
            });

            var entityKeyValues = Class.GetSampleKeyArguments().ToArray();
            var instancePath = Class.GetDefaultEntityPath(entityKeyValues);
            var responseKeyValues = Class.GetSampleKeyArguments().ToArray();
            var response = Class.GetSampleJObject(responseKeyValues);

            using (var mockService = new MockService())
            {
                mockService
                    .OnInvokeMethodRequest("GET",
                        instancePath + "/" + Method.FullName,
                        null,
                        null)
                    .RespondWithGetEntity(TargetEntity.Class.GetDefaultEntitySetName(), response);

                var fetcher = mockService
                    .GetDefaultContext(Model)
                    .CreateFetcher(FetcherType, instancePath);

                var result = fetcher.InvokeMethod<Task>(Method.Name + "Async").GetPropertyValue<EntityBase>("Result");

                result.ValidatePropertyValues(responseKeyValues);
            }
        }
    }
}
