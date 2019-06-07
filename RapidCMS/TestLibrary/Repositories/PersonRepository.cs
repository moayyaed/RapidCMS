﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RapidCMS.Common.Data;
using TestLibrary.Data;
using TestLibrary.Entities;

namespace TestLibrary.Repositories
{
    public class PersonRepository : BaseStructRepository<int, int, PersonEntity>
    {
        private readonly TestDbContext _dbContext;

        public PersonRepository(TestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task DeleteAsync(int id, int? parentId)
        {
            var entry = new CountryEntity { _Id = id };
            _dbContext.Countries.Remove(entry);
            await _dbContext.SaveChangesAsync();
        }

        public override async Task<IEnumerable<PersonEntity>> GetAllAsync(int? parentId)
        {
            return await _dbContext.Persons.Include(x => x.Countries).ThenInclude(x => x.Country).AsNoTracking().ToListAsync();
        }

        public override async Task<PersonEntity> GetByIdAsync(int id, int? parentId)
        {
            return await _dbContext.Persons.Include(x => x.Countries).ThenInclude(x => x.Country).AsNoTracking().FirstOrDefaultAsync(x => x._Id == id);
        }

        public override async Task<PersonEntity> InsertAsync(int? parentId, PersonEntity entity, IRelationContainer relations)
        {
            entity.Countries = relations.GetRelatedElementIdsFor<CountryEntity, int>().Select(id => new PersonCountryEntity { CountryId = id }).ToList();
            var entry = _dbContext.Persons.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entry.Entity;
        }

        public override Task<PersonEntity> NewAsync(int? parentId, Type variantType = null)
        {
            return Task.FromResult(new PersonEntity { Countries = new List<PersonCountryEntity>() });
        }

        public override int ParseKey(string id)
        {
            return int.Parse(id);
        }

        public override int? ParseParentKey(string parentId)
        {
            return int.TryParse(parentId, out var id) ? id : default(int?);
        }

        public override async Task UpdateAsync(int id, int? parentId, PersonEntity entity, IRelationContainer relations)
        {
            var dbEntity = await _dbContext.Persons.Include(x => x.Countries).FirstOrDefaultAsync(x => x._Id == id);

            dbEntity.Name = entity.Name;

            var newCountries = relations.GetRelatedElementIdsFor<CountryEntity, int>();

            foreach (var country in dbEntity.Countries.Where(x => !newCountries.Contains(x.CountryId.Value)).ToList())
            {
                dbEntity.Countries.Remove(country);
            }
            foreach (var countryId in newCountries.Where(id => !dbEntity.Countries.Select(x => x.CountryId.Value).Contains(id)).ToList())
            {
                dbEntity.Countries.Add(new PersonCountryEntity { CountryId = countryId });
            }

            _dbContext.Persons.Update(dbEntity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
