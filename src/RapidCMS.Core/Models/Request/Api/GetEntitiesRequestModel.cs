﻿using RapidCMS.Core.Abstractions.Data;
using RapidCMS.Core.Enums;

namespace RapidCMS.Core.Models.Request.Api
{
    public class GetEntitiesRequestModel
    {
        public UsageType UsageType { get; set; }
        public string RepositoryAlias { get; set; } = default!;
        public IQuery Query { get; set; } = default!;
    }
}
