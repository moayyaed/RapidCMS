﻿using System.Threading.Tasks;
using RapidCMS.Common.Data;
using RapidCMS.Common.Enums;

namespace RapidCMS.Common.ActionHandlers
{
    public interface IButtonActionHandler
    {
        CrudType GetCrudType();
        bool IsCompatibleWithView(ViewContext viewContext);
        bool ShouldConfirm();
        Task InvokeAsync(string? parentId, string? id, object? customData);
        bool RequiresValidForm();
    }
}
