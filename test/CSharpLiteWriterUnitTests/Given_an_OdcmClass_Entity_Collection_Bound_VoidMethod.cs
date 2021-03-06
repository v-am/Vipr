﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Its.Recipes;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Vipr.Core.CodeModel;
using Xunit;

namespace CSharpLiteWriterUnitTests
{
    public class Given_an_OdcmClass_Entity_Collection_Bound_VoidMethod : EntityTestBase
    {
        private OdcmMethod _method;
        private readonly Type _expectedReturnType = typeof(Task);
        private string _expectedMethodName;
        private IEnumerable<Type> _expectedMethodParameters;

        
        public Given_an_OdcmClass_Entity_Collection_Bound_VoidMethod()
        {
            _method = Any.OdcmMethod(m => m.IsBoundToCollection = true);

            _expectedMethodName = _method.Name + "Async";

            _expectedMethodParameters = _method.Parameters.Select(p => Proxy.GetClass(p.Type.Namespace, p.Type.Name));

            Init(m => m.Namespaces[0].Classes.First().Methods.Add(_method));
        }

        [Fact]
        public void The_Concrete_interface_does_not_expose_the_method()
        {
            ConcreteInterface.Should().NotHaveMethod(_expectedMethodName);
        }

        [Fact]
        public void The_Concrete_class_does_not_expose_the_method()
        {
            ConcreteType.Should().NotHaveMethod(_expectedMethodName);
        }

        [Fact]
        public void The_Fetcher_interface_does_not_expose_the_method()
        {
            FetcherInterface.Should().NotHaveMethod(_expectedMethodName);
        }

        [Fact]
        public void The_Fetcher_class_does_not_expose_the_method()
        {
            FetcherType.Should().NotHaveMethod(_expectedMethodName);
        }

        [Fact]
        public void The_Collection_interface_exposes_the_method()
        {
            CollectionInterface.Should().HaveMethod(
                CSharpAccessModifiers.Public,
                _expectedReturnType,
                _expectedMethodName,
                _expectedMethodParameters);
        }

        [Fact]
        public void The_Collection_class_exposes_the_async_method()
        {
            CollectionType.Should().HaveMethod(
            CSharpAccessModifiers.Public,
            true,
            _expectedReturnType,
            _expectedMethodName,
            _expectedMethodParameters);
        }
    }
}
