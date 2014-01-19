using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    public static class Extensions
    {
        /// <summary>
        /// If entity with the same key already in the ObjectContext, returns that entity.
        /// Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity">Entity with key</typeparam>
        /// <param name="db">EF ObjectContext</param>
        /// <param name="entity">Entity with key specified to attach or find loaded entity with the same key.</param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this ObjectContext db, TEntity entity) where TEntity : class, IEntityWithKey
        {
            if (entity == null)
                return null;

            if (db == null)
                throw new ArgumentNullException("db");

            ObjectStateEntry entry;

            if (db.ObjectStateManager.TryGetObjectStateEntry(entity, out entry))
            {
                TEntity loadedEntity = (TEntity)entry.Entity;
                if (entry.State == System.Data.EntityState.Detached)
                    db.Attach(loadedEntity);
                return loadedEntity;
            }

            db.Attach(entity);
            return entity;
        }

        /// <summary>
        /// If entity with the same key is already in the DbContext, returns that entity.
        /// Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        /// <param name="entityKeyRetriever">Method returning entity field (or collection of fields) that make up entity key.</param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this DbContext db, TEntity entity, Func<TEntity, object> entityKeyRetriever) where TEntity : class
        {
            if (entity == null)
                return entity;

            if (db == null)
                throw new ArgumentNullException("db");

            if (entityKeyRetriever == null)
                throw new ArgumentNullException("entityKeyRetriever");

            object key = entityKeyRetriever(entity);
            if (key == null)
                throw new ArgumentException("Entity must have a non-null key field or collection of fields.");

            DbSet<TEntity> table = db.Set<TEntity>();

            TEntity existing = table.Local.Where(loadedEnt => key.Equals(entityKeyRetriever(loadedEnt))).FirstOrDefault();

            if (existing != null)
                return existing;

            table.Attach(entity);
            return entity;
        }

        /// <summary>
        /// If entity with the same key is already in the DbContext, returns that entity.
        /// Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity">Entity implementing IDbEntityKey interface.</typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this DbContext db, TEntity entity) where TEntity : class, IDbEntityKey
        {
            return db.GetOrAttach<TEntity>(entity, ent => ent.DbContextEntityKey);
        }

        /// <summary>
        /// Marks entity as Deleted.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db"></param>
        /// <param name="entity"></param>
        public static void DeleteEntity<TEntity>(this ObjectContext db, TEntity entity) where TEntity : class, IEntityWithKey
        {
            entity = db.GetOrAttach(entity);
            db.DeleteObject(entity);
        }

        /// <summary>
        /// Marks entity as Deleted. 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        /// <param name="entityKeyRetriever">Method returning entity field (or collection of fields) that make up entity key.</param>
        public static void DeleteEntity<TEntity>(this DbContext db, TEntity entity, Func<TEntity, object> entityKeyRetriever) where TEntity : class
        {
            entity = db.GetOrAttach(entity, entityKeyRetriever);
            db.Set<TEntity>().Remove(entity);
        }

        /// <summary>
        /// Marks entity as Deleted. 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        public static void DeleteEntity<TEntity>(this DbContext db, TEntity entity) where TEntity : class, IDbEntityKey
        {
            db.DeleteEntity(entity, ent => ent.DbContextEntityKey);
        }
    }
}
