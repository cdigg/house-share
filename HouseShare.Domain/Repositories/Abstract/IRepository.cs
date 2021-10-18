using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HouseShare.Domain.Repositories.Abstract
{
    /// <summary>
    /// Defines an a standard Repository interface.
    /// 
    /// Code borrowed/adapted from: http://www.codeproject.com/Articles/207820/The-Repository-Pattern-with-EF-code-first-Dependen
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepository<T> : IDisposable where T : class
    {
        /// <summary>
        /// Get a queryable entity collection
        /// </summary>
        /// <returns></returns>
        IQueryable<T> Get { get; }

        /// <summary>
        /// Find an object by key lookup
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        T Find(params object[] keys);

        T FindForUpdate(T t, params object[] keys);

        /// <summary>
        /// Add a new object to the repository
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        T Add(T t);

        /// <summary>
        /// Remove an object from the repository
        /// </summary>
        /// <param name="t"></param>
        void Remove(T t);

        /// <summary>
        /// Remove all objects from the repository that match a specified filter condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        int Remove(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Attach the specified object to the repository and mark it as changed.
        /// </summary>
        /// <param name="t"></param>
        void Changed(T t);

        /// <summary>
        /// Commit changes to the repository. Use this method if not using an IUnitOfWork with multiple repositories.
        /// </summary>
        void SaveChanges();
    }
}
