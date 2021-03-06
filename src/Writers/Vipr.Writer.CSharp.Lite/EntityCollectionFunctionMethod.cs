﻿using Vipr.Core.CodeModel;

namespace Vipr.Writer.CSharp.Lite
{
    internal class EntityCollectionFunctionMethod : EntityInstanceFunctionMethod
    {
        public EntityCollectionFunctionMethod(OdcmMethod odcmMethod) : base(odcmMethod)
        {
            ReturnType = Type.TaskOf(Type.IEnmerableOf(new Type(NamesService.GetPublicTypeName(odcmMethod.ReturnType))));
        }
    }
}