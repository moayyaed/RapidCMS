﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RapidCMS.Core.Abstractions.Data;
using RapidCMS.Core.Abstractions.Dispatchers;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Abstractions.Services;
using RapidCMS.Core.Abstractions.Setup;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Extensions;
using RapidCMS.Core.Forms;
using RapidCMS.Core.Models.Request.Form;

namespace RapidCMS.Core.Dispatchers.Form
{
    internal class GetEntitiesDispatcher : IPresentationDispatcher<GetEntitiesRequestModel, ListContext>
    {
        private readonly ISetupResolver<ICollectionSetup> _collectionResolver;
        private readonly IRepositoryResolver _repositoryResolver;
        private readonly IDataViewResolver _dataViewResolver;
        private readonly IParentService _parentService;
        private readonly IConcurrencyService _concurrencyService;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;

        public GetEntitiesDispatcher(
            ISetupResolver<ICollectionSetup> collectionResolver,
            IRepositoryResolver repositoryResolver,
            IDataViewResolver dataViewResolver,
            IParentService parentService,
            IConcurrencyService concurrencyService,
            IAuthService authService,
            IServiceProvider serviceProvider)
        {
            _collectionResolver = collectionResolver;
            _repositoryResolver = repositoryResolver;
            _dataViewResolver = dataViewResolver;
            _parentService = parentService;
            _concurrencyService = concurrencyService;
            _authService = authService;
            _serviceProvider = serviceProvider;
        }

        public async Task<ListContext> GetAsync(GetEntitiesRequestModel request)
        {
            var collection = _collectionResolver.ResolveSetup(request.CollectionAlias);
            var repository = _repositoryResolver.GetRepository(collection);

            var parent = request is GetEntitiesOfParentRequestModel parentRequest ? await _parentService.GetParentAsync(parentRequest.ParentPath).ConfigureAwait(false) : default;
            var relatedEntity = (request as GetEntitiesOfRelationRequestModel)?.Related;

            var protoEntity = await _concurrencyService.EnsureCorrectConcurrencyAsync(() => repository.NewAsync(parent, collection.EntityVariant.Type)).ConfigureAwait(false);
            var newEntity = request.VariantAlias == collection.EntityVariant.Alias
                ? protoEntity
                : await _concurrencyService.EnsureCorrectConcurrencyAsync(() => repository.NewAsync(parent, collection.GetEntityVariant(request.VariantAlias).Type)).ConfigureAwait(false);

            await _authService.EnsureAuthorizedUserAsync(request.UsageType, protoEntity).ConfigureAwait(false);
            await _dataViewResolver.ApplyDataViewToQueryAsync(request.Query).ConfigureAwait(false);

            var action = (request.UsageType & ~(UsageType.List | UsageType.Root | UsageType.NotRoot)) switch
            {
                UsageType.Add when relatedEntity != null => () => repository.GetAllNonRelatedAsync(relatedEntity!, request.Query),
                _ when relatedEntity != null => () => repository.GetAllRelatedAsync(relatedEntity!, request.Query),
                _ when relatedEntity == null => () => repository.GetAllAsync(parent, request.Query),

                _ => default(Func<Task<IEnumerable<IEntity>>>)
            };

            if (action == default)
            {
                throw new InvalidOperationException($"UsageType {request.UsageType} is invalid for this method");
            }

            var existingEntities = await _concurrencyService.EnsureCorrectConcurrencyAsync(action).ConfigureAwait(false);
            var protoEditContext = new EditContext(request.CollectionAlias, collection.RepositoryAlias, protoEntity, parent, request.UsageType | UsageType.List, _serviceProvider);

            return new ListContext(
                request.CollectionAlias,
                protoEditContext,
                parent,
                request.UsageType,
                ConvertEditContexts(request, protoEditContext, existingEntities),
                _serviceProvider);
        }

        private List<EditContext> ConvertEditContexts(
            GetEntitiesRequestModel request,
            EditContext protoEditContext,
            IEnumerable<IEntity> existingEntities)
        {
            if (request.UsageType.HasFlag(UsageType.Add))
            {
                return existingEntities
                    .Select(ent => new EditContext(request.CollectionAlias, protoEditContext.RepositoryAlias, ent, protoEditContext.Parent, UsageType.Node | UsageType.Pick, _serviceProvider))
                    .ToList();
            }
            else if (request.UsageType.HasFlag(UsageType.Edit) || request.UsageType.HasFlag(UsageType.New))
            {
                var entities = existingEntities
                    .Select(ent => new EditContext(request.CollectionAlias, protoEditContext.RepositoryAlias, ent, protoEditContext.Parent, UsageType.Node | UsageType.Edit, _serviceProvider))
                    .ToList();

                if (request.UsageType.HasFlag(UsageType.New))
                {
                    entities.Insert(0, new EditContext(request.CollectionAlias, protoEditContext.RepositoryAlias, protoEditContext.Entity, protoEditContext.Parent, UsageType.Node | UsageType.New, _serviceProvider));
                }

                return entities;
            }
            else if (request.UsageType.HasFlag(UsageType.View))
            {
                return existingEntities
                    .Select(ent => new EditContext(request.CollectionAlias, protoEditContext.RepositoryAlias, ent, protoEditContext.Parent, UsageType.Node | UsageType.View, _serviceProvider))
                    .ToList();
            }
            else
            {
                throw new NotImplementedException($"Failed to process {request.UsageType} for collection {request.CollectionAlias}");
            }
        }
    }
}
