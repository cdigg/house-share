using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HouseShare.Domain.Repositories.Abstract;

namespace HouseShare.Domain.Repositories.Concrete
{
    /// <summary>
    /// Generic repository implemented using a DbContext.
    /// </summary>
    /// <remarks>
    /// Code adapted from: http://www.codeproject.com/Articles/207820/The-Repository-Pattern-with-EF-code-first-Dependen
    /// 
    /// This implementation allows for using a shared DbContext across multiple repositires.
    /// SaveChanges is the only method that writes changes to the database.
    /// DbContext.SaveChanges should be be called in the injected context when using a shared conext.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class DbRepository<T, TKey> : IRepository<T>
        where T : class
    {
        // This is public so it accessible from unit tests to confirm that dependency injection worked correctly.
        public DbContext Context { get; set; }

        public DbRepository(DbContext context)
        {
            this.Context = context;
        }

        public virtual void Dispose()
        {
            Context.Dispose();
        }

        /// <summary>
        /// Used for update operations.
        /// </summary>
        /// <remarks>
        /// When using entity inheritance, override DbSet to return the set for the base entity.
        /// </remarks>
        protected virtual DbSet DbSet
        {
            get { return Context.Set<T>(); }
        }

        /// <summary>
        /// Used for query operations.
        /// </summary>
        /// <remarks>
        /// When using entity inheritance, override FetchQuery to return BaseEntity.OfType<SubClass>()
        /// Example: get { return ((AmesDbContext)Context)).Companies.OfType<OilCompany>(); }
        /// </remarks>
        protected virtual IQueryable<T> FetchQuery
        {
            get { return Context.Set<T>(); }
        }


        /// <summary>
        /// Get a queryable entity collection
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> Get
        {
            get { return FetchQuery; }
        }

        /// <summary>
        /// Find an object by key lookup
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual T Find(params object[] keys)
        {
            return DbSet.Find(keys) as T;
        }

        /// <summary>
        /// Find object by key lookup
        /// Set changed values from "t" 
        /// Return object with changed values
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual T FindForUpdate(T t, params object[] keys)
        {
            var entity = DbSet.Find(keys) as T;

            // loop through properties that are not null (changed values)
            var properties = t.GetType().GetProperties().Where(v => v.GetValue(t) != null && (!v.Name.Contains("CreatedDate") && !v.Name.Contains("CreatedById")));// && !v.PropertyType.Name.Contains("ICollection"));//t.GetType().GetProperties().Where(v => v.GetValue(t) != null && !v.PropertyType.Name.Contains("ICollection") && !v.PropertyType.IsClass);

            foreach (var propertyInfo in properties)
            {
                //entity.GetType().GetProperty(propertyInfo.Name).SetValue(t, 5);
                //entity.GetType().GetProperty(propertyInfo.Name).GetValue(entity);
                if (propertyInfo.PropertyType.Name.Contains("Int") && (int)t.GetType().GetProperty(propertyInfo.Name).GetValue(t) < 1)
                {
                    // skip
                }
                else
                    entity.GetType().GetProperty(propertyInfo.Name).SetValue(entity, t.GetType().GetProperty(propertyInfo.Name).GetValue(t));
            }


            return entity;
        }

        /// <summary>
        /// Add an object to the repository
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual T Add(T t)
        {
            return DbSet.Add(t) as T;
        }

        /// <summary>
        /// Remove an object from the repository
        /// </summary>
        /// <param name="t"></param>
        public virtual void Remove(T t)
        {
            DbSet.Remove(t);
        }

        /// <summary>
        /// Remove all objects from the repository that match a specified filter condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual int Remove(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            int cnt = 0;
            foreach (T obj in FetchQuery.Where(predicate).ToList())
            {
                DbSet.Remove(obj);
                cnt++;
            }
            return cnt;
        }

        /// <summary>
        /// Attach the specified object to the repository and mark it as changed.
        /// Note: Properties with Editable data annotation set to false will not
        ///       be flagged as modified.
        /// Note: Complex types will not be flagged as modified.
        ///       This should be fixed in a future version.
        /// </summary>
        /// <param name="t"></param>
        public virtual void Changed(T t)
        {
            setModified(t);
        }

        /// <summary>
        /// Write chanegs to the database. Use this method if not sharing DbContext across multiple repositories.
        /// </summary>
        public virtual void SaveChanges()
        {
            Context.SaveChanges();
        }

        public virtual void Detatch(T t)
        {
            ((System.Data.Entity.Infrastructure.IObjectContextAdapter)this.Context).ObjectContext.Detach(t);
        }

        /*
        private void setModifiedTemp(T entity)
        {
            var entry = Context.Entry(entity);
            DbSet.Attach(entity);
            entry.State = EntityState.Modified;
        }
         * */


        private void setModified(T entity)
        {
            // Get list of properties
            var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

            // Get list of properties where EditableAttribute.AllowEdit == false
            var readOnlyProperties = properties.Where(
                p => p.GetCustomAttributes(typeof(EditableAttribute), true).Where(a => ((EditableAttribute)a).AllowEdit == false).Any()
                ).ToList();

            // Check for MetadataType attribute
            MetadataTypeAttribute ma = entity.GetType().GetCustomAttributes(typeof(MetadataTypeAttribute), true).OfType<MetadataTypeAttribute>().FirstOrDefault();
            if (ma != null)
            {
                // Check Metadata Type for readonly properties
                var metaProperties = ma.MetadataClassType.GetProperties();
                var propNames = metaProperties.Where(p => p.GetCustomAttributes(typeof(EditableAttribute), true).Where(a => ((EditableAttribute)a).AllowEdit == false).Any()).Select(p => p.Name).ToList();
                readOnlyProperties.AddRange(
                    entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => propNames.Contains(p.Name))
                    );
            }

            // Mark whole entity as Modified, when collection of readonly properties is empty
            if (readOnlyProperties.Count() == 0)
            {
                Context.Entry(entity).State = EntityState.Modified;
                return;
            }
            else
                Context.Entry(entity).State = EntityState.Detached;

            // Attach entity to DbContext
            DbSet.Attach(entity);

            var propertiesForUpdate = properties.Except(readOnlyProperties);
            foreach (var propertyInfo in propertiesForUpdate)
            {
                // Skip navigation properties
                // Note: I don't think this will catch complex types. We need to put in a test for complex types or find another way
                //       to skip navigation properties.
                Type t = propertyInfo.PropertyType;
                if (t.IsPrimitive || t.IsValueType || t == typeof(string))
                {
                    Context.Entry(entity).Property(propertyInfo.Name).IsModified = true;
                }
            }
        }
    }
}
