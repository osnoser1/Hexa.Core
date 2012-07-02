﻿#region License

// ===================================================================================
// Copyright 2010 HexaSystems Corporation
// ===================================================================================
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// ===================================================================================
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// See the License for the specific language governing permissions and
// ===================================================================================

#endregion

using System;
using FluentNHibernate;
using FluentNHibernate.Conventions;

namespace Hexa.Core.Domain
{
    public class ForeignKeyColumnNames : ForeignKeyConvention
    {
        protected override string  GetKeyName(Member property, Type type)
        {
            // many-to-many, one-to-many, join
            if (property == null)
                {
                    if (type.GetProperty("UniqueId") != null)
                        return type.Name + "UniqueId";

                    return type.Name + "Id";
                }

            // else -- many-to-one
            if (property.PropertyType.GetProperty("UniqueId") != null)
                return property.Name + "UniqueId";

            return property.Name + "Id";
        }
    }
}
