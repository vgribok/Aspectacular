#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Data.SqlClient;
using System.Linq;

namespace Aspectacular
{
    public static class EfExtensions
    {
        /// <summary>
        ///     If entity with the same key already in the ObjectContext, returns that entity.
        ///     Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity">Entity with key</typeparam>
        /// <param name="db">EF ObjectContext</param>
        /// <param name="entity">Entity with key specified to attach or find loaded entity with the same key.</param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this ObjectContext db, TEntity entity)
            where TEntity : class, IEntityWithKey
        {
            if(entity == null)
                return null;

            if(db == null)
                throw new ArgumentNullException("db");

            ObjectStateEntry entry;

            if(db.ObjectStateManager.TryGetObjectStateEntry(entity, out entry))
            {
                TEntity loadedEntity = (TEntity)entry.Entity;
                if(entry.State == EntityState.Detached)
// ReSharper disable once AssignNullToNotNullAttribute
                    db.Attach(loadedEntity);
                return loadedEntity;
            }

            db.Attach(entity);
            return entity;
        }

        /// <summary>
        ///     If entity with the same key is already in the DbContext, returns that entity.
        ///     Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified. Something like: "new EntityClass { ID = 5 }"</param>
        /// <param name="entityKeyRetriever">
        ///     Method returning entity field (or collection of fields) that make up entity key.
        ///     Something like "record => record.ID"
        /// </param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this DbContext db, TEntity entity, Func<TEntity, object> entityKeyRetriever) where TEntity : class
        {
            if(entity == null)
                return null;

            if(db == null)
                throw new ArgumentNullException("db");

            TEntity existing = db.LoadEntityByKey(entity, entityKeyRetriever);

            if(existing != null)
                return existing;

            DbSet<TEntity> table = db.Set<TEntity>();
            table.Attach(entity);
            return entity;
        }

        /// <summary>
        ///     If entity with the same key is already in the DbContext, returns that entity.
        ///     Otherwise attaches passed entity and returns it.
        /// </summary>
        /// <typeparam name="TEntity">Entity implementing IDbEntityKey interface.</typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        /// <returns></returns>
        public static TEntity GetOrAttach<TEntity>(this DbContext db, TEntity entity) where TEntity : class, IDbEntityKey
        {
            return db.GetOrAttach(entity, ent => ent.DbContextEntityKey);
        }

        /// <summary>
        ///     Loads and returns entity with a given key.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">Entity Framework DbContext.</param>
        /// <param name="entity">Entity with only a key specified. Something like: "new EntityClass { ID = 5 }"</param>
        /// <param name="entityKeyRetriever">
        ///     Method returning entity field (or collection of fields) that make up entity key.
        ///     Something like "record => record.ID"
        /// </param>
        /// <returns></returns>
        public static TEntity LoadEntityByKey<TEntity>(this DbContext db, TEntity entity, Func<TEntity, object> entityKeyRetriever) where TEntity : class
        {
            if(entityKeyRetriever == null)
                throw new ArgumentNullException("entityKeyRetriever");

            object key = entityKeyRetriever(entity);
            if(key == null)
                throw new ArgumentException("Entity must have a non-null key field or collection of fields.");

            DbSet<TEntity> table = db.Set<TEntity>();
            TEntity existing = table.Local.FirstOrDefault(loadedEnt => key.Equals(entityKeyRetriever(loadedEnt)));
            return existing;
        }

        /// <summary>
        ///     Loads and returns entity with a given key.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">Entity Framework DbContext.</param>
        /// <param name="entity">Entity with only a key specified. Something like: "new EntityClass { ID = 5 }"</param>
        /// <returns></returns>
        public static TEntity LoadEntityByKey<TEntity>(this DbContext db, TEntity entity)
            where TEntity : class, IDbEntityKey
        {
            return db.LoadEntityByKey(entity, ent => ent.DbContextEntityKey);
        }

        /// <summary>
        ///     Adds new entity to DB context.
        ///     SaveChanges() needs to be called afterwards.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static TEntity AddEntity<TEntity>(this DbContext db, TEntity entity) where TEntity : class
        {
            if(entity != null)
                db.AddEntities(new[] {entity});

            return entity;
        }

        /// <summary>
        ///     Adds multiple new entities of the same type to DB context.
        ///     SaveChanges() needs to be called afterwards.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db"></param>
        /// <param name="entities"></param>
        public static void AddEntities<TEntity>(this DbContext db, IEnumerable<TEntity> entities) where TEntity : class
        {
            if(db == null)
                throw new ArgumentNullException("db");

            DbSet<TEntity> table = db.Set<TEntity>();

            entities.ForEach(entity => table.Add(entity));
        }

        /// <summary>
        ///     Marks entity as Deleted.
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
        ///     Marks entity as Deleted.
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
        ///     Marks entity as Deleted.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="db">EF DbContext</param>
        /// <param name="entity">Entity with only a key specified</param>
        public static void DeleteEntity<TEntity>(this DbContext db, TEntity entity) where TEntity : class, IDbEntityKey
        {
            db.DeleteEntity(entity, ent => ent.DbContextEntityKey);
        }

        /// <summary>
        ///     If DbContext is for SQL Server,
        ///     returns SqlConnection. Otherwise returns null.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SqlConnection GetSqlConnection(this DbContext db)
        {
            if(db == null || db.Database == null)
                return null;

            SqlConnection sqlConnection = db.Database.Connection as SqlConnection;
            return sqlConnection;
        }

        /// <summary>
        ///     If ObjectContext is for SQL Server,
        ///     returns SqlConnection. Otherwise returns null.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SqlConnection GetSqlConnection(this ObjectContext db)
        {
            if(db == null)
                return null;

            var entityConnection = db.Connection as EntityConnection;
            if(entityConnection == null)
                return null;

            var sqlConnection = entityConnection.StoreConnection as SqlConnection;
            return sqlConnection;
        }
    }
}