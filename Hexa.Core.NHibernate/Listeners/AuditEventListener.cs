﻿#region License

//===================================================================================
//Copyright 2010 HexaSystems Corporation
//===================================================================================
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//http://www.apache.org/licenses/LICENSE-2.0
//===================================================================================
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//===================================================================================

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Persister.Entity;
using Hexa.Core.Security;

namespace Hexa.Core.Domain
{
    public class AuditEventListener : IPreUpdateEventListener, IPreInsertEventListener
    {
        public bool OnPreInsert(PreInsertEvent e)
        {
            var auditable = e.Entity as IAuditableEntity;
            if (auditable == null)
                return false;

            var identity = ApplicationContext.User.Identity as ICoreIdentity;
            Guard.IsNotNull(identity, "No ICoreIdentity found in context.");
            var id = identity.Id;

            var date = DateTime.Now;

            _Set(e.Persister, e.State, "CreatedBy", id);
            _Set(e.Persister, e.State, "UpdatedBy", id);
            _Set(e.Persister, e.State, "CreatedAt", date);
            _Set(e.Persister, e.State, "UpdatedAt", date);

            auditable.CreatedBy = id;
            auditable.UpdatedBy = id;
            auditable.CreatedAt = date;
            auditable.UpdatedAt = date;

            return false;
        }

        public bool OnPreUpdate(PreUpdateEvent e)
        {
            var auditable = e.Entity as IAuditableEntity;
            if (auditable == null)
                return false;

            var logger = ServiceLocator.TryGetInstance<IAuditableEntityLogger>();
            if (logger != null)
            {
                var changedPropertiesIdx = e.Persister.FindDirty(e.State, e.OldState, e.Entity, e.Session.GetSessionImplementation());
                foreach (var idx in changedPropertiesIdx)
                {
                    var tableName = e.Persister.EntityName;
                    var propertyName = e.Persister.PropertyNames[idx];
                    var oldValue = e.OldState[idx];
                    var newValue = e.State[idx];

                    logger.Log(tableName, propertyName, oldValue, newValue);
                }
            }

            var identity = ApplicationContext.User.Identity as ICoreIdentity;
            Guard.IsNotNull(identity, "No ICoreIdentity found in context.");
            var id = identity.Id;

            var date = DateTime.Now;

            _Set(e.Persister, e.State, "UpdatedBy", id);
            _Set(e.Persister, e.State, "UpdatedAt", date);
            auditable.UpdatedBy = id;
            auditable.UpdatedAt = date;

            return false;
        }

        private void _Set(IEntityPersister persister, object[] state, string propertyName, object value)
        {
            var index = Array.IndexOf(persister.PropertyNames, propertyName);
            if (index == -1)
                return;
            state[index] = value;
        }
    }

    //http://stackoverflow.com/questions/5087888/ipreupdateeventlistener-and-dynamic-update-true
    public class AuditFlushEntityEventListener : DefaultFlushEntityEventListener
    {
        protected override void DirtyCheck(FlushEntityEvent e)
        {
            base.DirtyCheck(e);
            if (e.DirtyProperties != null &&
                e.DirtyProperties.Any() &&
                //IAuditableEntity is my inteface for audited entities
                e.Entity is IAuditableEntity)
                e.DirtyProperties = e.DirtyProperties
                 .Concat(_GetAdditionalDirtyProperties(e)).ToArray();
        }

        private static IEnumerable<int> _GetAdditionalDirtyProperties(FlushEntityEvent @event)
        {
            yield return Array.IndexOf(@event.EntityEntry.Persister.PropertyNames,
                                       "UpdatedAt");
            yield return Array.IndexOf(@event.EntityEntry.Persister.PropertyNames,
                                       "UpdatedBy");
            yield return Array.IndexOf(@event.EntityEntry.Persister.PropertyNames,
                                       "CreatedBy");
            yield return Array.IndexOf(@event.EntityEntry.Persister.PropertyNames,
                                       "CreatedAt");
        }
    }
}
