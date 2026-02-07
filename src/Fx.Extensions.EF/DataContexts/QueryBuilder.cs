// ------------------------------------------------------------------------------------------------
// All rights reserved. This project and this file is published under the Apache License 2. So, Any
// use of this project must comply with the terms and policies described in the project’s `LICENSE`
// file. For more information, please visit the project repository on GitHub or contact with owner.
// ------------------------------------------------------------------------------------------------

using Fx.Common.Domains.Repositories;
using Fx.Common.Domains.Specifications;
using Fx.Common.Models;

namespace Fx.Extensions.EF.DataContexts;

/// <summary>
/// Build an IQueryable object from a specification object and other relevant parameters.
/// </summary>
public class QueryBuilder
{
    /// <summary>
    /// Makes a <see cref="IQueryable{TModel}"/> object from input parameters.
    /// </summary>
    /// <typeparam name="TModel">Type of the query data model.</typeparam>
    /// <param name="source">A queryable object that will be used as the base of the query.</param>
    /// <param name="pageSize">The number of records to be returned.</param>
    /// <param name="pageIndex">The index of a page to be returned in the result.</param>
    /// <param name="sorter">An object that specifies how records should be ordered.</param>
    /// <param name="specification">A specification that should be used to make a criteria.</param>
    /// <returns>An <see cref="IQueryable{TModel}"/> object.</returns>
    public IQueryable<TModel> Build<TModel>(
        IQueryable<TModel> source,
        int pageSize,
        int pageIndex,
        Sorter<TModel> sorter,
        ISpecification<TModel> specification)
        where TModel : class, IDataModel
    {
        if (specification is not null)
        {
            source = source.Where(specification.Export());
        }

        source = this.SetOrder<TModel>(source, sorter, isMain: true)
                     .Skip(pageIndex * pageSize)
                     .Take(pageSize);

        return source;
    }

    private IQueryable<TModel> SetOrder<TModel>(
        IQueryable<TModel> query,
        Sorter<TModel> sorter,
        bool isMain)
        where TModel : class, IDataModel
    {
        if (sorter.Direction is SortingDirections.ASCENDING)
        {
            query = isMain ? query.OrderBy(sorter.Column)
                           : ((IOrderedQueryable<TModel>)query).ThenBy(sorter.Column);
        }
        else
        {
            query = isMain ? query.OrderByDescending(sorter.Column)
                           : ((IOrderedQueryable<TModel>)query).ThenByDescending(sorter.Column);
        }

        if (sorter.Next is not null)
        {
            query = this.SetOrder<TModel>(query, sorter.Next, false);
        }

        return query;
    }
}
