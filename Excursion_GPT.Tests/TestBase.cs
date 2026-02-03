using Excursion_GPT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Excursion_GPT.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly AppDbContext _dbContext;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly ServiceProvider _serviceProvider;

        protected TestBase()
        {
            // Create a fresh service provider, and therefore a fresh
            // InMemory database instance for each test
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
            _options = _serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();

            // Ensure the database is created
            _dbContext.Database.EnsureCreated();
        }

        protected AppDbContext CreateNewContext()
        {
            return new AppDbContext(_options);
        }

        protected async Task<T> SaveEntityAsync<T>(T entity) where T : class
        {
            _dbContext.Set<T>().Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        protected async Task<List<T>> SaveEntitiesAsync<T>(IEnumerable<T> entities) where T : class
        {
            var entityList = entities.ToList();
            _dbContext.Set<T>().AddRange(entityList);
            await _dbContext.SaveChangesAsync();
            return entityList;
        }

        protected async Task ClearDatabaseAsync()
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
            _dbContext.Buildings.RemoveRange(_dbContext.Buildings);
            _dbContext.Models.RemoveRange(_dbContext.Models);
            _dbContext.Tracks.RemoveRange(_dbContext.Tracks);
            _dbContext.Points.RemoveRange(_dbContext.Points);
            await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
