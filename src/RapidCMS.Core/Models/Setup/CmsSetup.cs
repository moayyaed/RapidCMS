﻿using RapidCMS.Core.Abstractions.Config;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Abstractions.Setup;
using RapidCMS.Core.Models.Config;

namespace RapidCMS.Core.Models.Setup
{
    internal class CmsSetup : ICms, ILogin
    {
        private readonly ISetupResolver<ITypeRegistration, CustomTypeRegistrationConfig> _typeRegistrationSetupResolver;

        public CmsSetup(
            CmsConfig config,
            ISetupResolver<ITypeRegistration, CustomTypeRegistrationConfig> typeRegistrationSetupResolver)
        {
            _typeRegistrationSetupResolver = typeRegistrationSetupResolver;

            // TODO: resolve?
            SiteName = config.SiteName;
            IsDevelopment = config.IsDevelopment;

            if (config.CustomLoginScreenRegistration != null)
            {
                CustomLoginScreenRegistration = _typeRegistrationSetupResolver.ResolveSetup(config.CustomLoginScreenRegistration).Setup;
            }
            if (config.CustomLoginStatusRegistration != null)
            {
                CustomLoginStatusRegistration = _typeRegistrationSetupResolver.ResolveSetup(config.CustomLoginStatusRegistration).Setup;
            }
        }

        internal string SiteName { get; set; }
        internal bool IsDevelopment { get; set; }

        public ITypeRegistration? CustomLoginScreenRegistration { get; internal set; }
        public ITypeRegistration? CustomLoginStatusRegistration { get; internal set; }

        string ICms.SiteName => SiteName;
        bool ICms.IsDevelopment
        {
            get => IsDevelopment;
            set => IsDevelopment = value;
        }
    }
}
