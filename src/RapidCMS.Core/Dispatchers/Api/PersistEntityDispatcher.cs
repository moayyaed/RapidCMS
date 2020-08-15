﻿using System;
using System.Threading.Tasks;
using RapidCMS.Core.Abstractions.Dispatchers;
using RapidCMS.Core.Abstractions.Factories;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Abstractions.Services;
using RapidCMS.Core.Abstractions.State;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Exceptions;
using RapidCMS.Core.Models.Data;
using RapidCMS.Core.Models.Request.Api;
using RapidCMS.Core.Models.Response;

namespace RapidCMS.Core.Dispatchers.Api
{
    internal class PersistEntityDispatcher : IInteractionDispatcher<PersistEntityRequestModel, ApiCommandResponseModel>
    {
        private readonly IAuthService _authService;
        private readonly IRepositoryResolver _repositoryResolver;
        private readonly IParentService _parentService;
        private readonly IEditContextFactory _editContextFactory;

        public PersistEntityDispatcher(
            IAuthService authService,
            IRepositoryResolver repositoryResolver,
            IParentService parentService,
            IEditContextFactory editContextFactory)
        {
            _authService = authService;
            _repositoryResolver = repositoryResolver;
            _parentService = parentService;
            _editContextFactory = editContextFactory;
        }

        public async Task<ApiCommandResponseModel> InvokeAsync(PersistEntityRequestModel request, IPageState pageState)
        {
            if (string.IsNullOrWhiteSpace(request.Descriptor.RepositoryAlias))
            {
                throw new ArgumentNullException();
            }

            var parent = await _parentService.GetParentAsync(ParentPath.TryParse(request.Descriptor.ParentPath));

            var subjectRepository = _repositoryResolver.GetRepository(request.Descriptor.RepositoryAlias);
            var referenceEntity = (request.EntityState == EntityState.IsExisting)
                ? await subjectRepository.GetByIdAsync(request.Descriptor.Id ?? throw new InvalidOperationException("Cannot modify entity without giving an Id."), parent)
                : await subjectRepository.NewAsync(parent, request.Entity.GetType());

            if (referenceEntity == null)
            {
                throw new NotFoundException("Reference entity is null");
            }

            var usageType = UsageType.Node | (request.EntityState == EntityState.IsNew ? UsageType.New : UsageType.Edit);

            await _authService.EnsureAuthorizedUserAsync(usageType, request.Entity);

            var editContext = _editContextFactory.GetEditContextWrapper(
                usageType,
                request.EntityState, 
                request.Entity, 
                referenceEntity, 
                parent,
                request.Relations);

            try
            {
                if (!editContext.IsValid())
                {
                    throw new InvalidEntityException();
                }

                if (request.EntityState == EntityState.IsNew)
                {
                    return new ApiPersistEntityResponseModel
                    {
                        NewEntity = await subjectRepository.InsertAsync(editContext)
                    };
                }
                else if (request.EntityState == EntityState.IsExisting)
                {
                    await subjectRepository.UpdateAsync(editContext);

                    return new ApiCommandResponseModel();
                }
                else
                {
                    throw new InvalidOperationException("Invalid usage type");
                }
            }
            catch (InvalidEntityException)
            {
                return new ApiPersistEntityResponseModel
                {
                    ValidationErrors = editContext.ValidationErrors
                };
            }
        }
    }
}
