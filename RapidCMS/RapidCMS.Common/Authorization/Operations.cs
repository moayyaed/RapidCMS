﻿using System;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using RapidCMS.Common.Enums;

namespace RapidCMS.Common.Authorization
{
    public static class Operations
    {
        /// <summary>
        /// Operation with no data action (refresh data, return to previous view, etc)
        /// </summary>
        public static OperationAuthorizationRequirement None = new OperationAuthorizationRequirement { Name = nameof(None) };

        /// <summary>
        /// Read-only viewing of entity
        /// </summary>
        public static OperationAuthorizationRequirement Read = new OperationAuthorizationRequirement { Name = nameof(Read) };

        /// <summary>
        /// Creating a new instance of entity
        /// </summary>
        public static OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = nameof(Create) };

        /// <summary>
        /// Modifying an existing instance of entity
        /// </summary>
        public static OperationAuthorizationRequirement Update = new OperationAuthorizationRequirement { Name = nameof(Update) };

        /// <summary>
        /// Deleting an existing instance of entity
        /// </summary>
        public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = nameof(Delete) };

        /// <summary>
        /// Adding a relation to an existing entity
        /// </summary>
        public static OperationAuthorizationRequirement Add = new OperationAuthorizationRequirement { Name = nameof(Add) };

        /// <summary>
        /// Removing an existing relation to an existing entity
        /// </summary>
        public static OperationAuthorizationRequirement Remove = new OperationAuthorizationRequirement { Name = nameof(Remove) };
        
        public static OperationAuthorizationRequirement GetOperationForCrudType(CrudType type)
        {
            return type switch
            {
                CrudType.None => None,
                CrudType.View => Read,
                CrudType.Create => Create,
                CrudType.Edit => Update,
                CrudType.Insert => Create,
                CrudType.Update => Update,
                CrudType.Delete => Delete,
                CrudType.Add => Add,
                CrudType.Remove => Remove,
                CrudType.Pick => Add,
                CrudType.Refresh => None,
                CrudType.Return => None,
                _ => throw new InvalidOperationException($"Operation of type {type} is not supported.")
            };
        }

        public static OperationAuthorizationRequirement GetOperationForUsageType(UsageType type)
        {
            return type switch
            {
                UsageType.Add => Add,
                UsageType.Edit => Update,
                UsageType.List => Read,
                UsageType.New => Create,
                UsageType.Node => Update,
                UsageType.Pick => Add,
                UsageType.View => Read,
                _ => throw new InvalidOperationException($"Operation of type {type} is not supported.")
            };
        }
    }
}
