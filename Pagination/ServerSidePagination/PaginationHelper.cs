using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServerSidePagination
{
    public class PaginationHelper<T>
    {
        public static PaginationResult<T> ApplyPagination(IQueryable<T> sourceData, int currentPage, int pageSize, dynamic searchKeyword, string sortColumn, string sortOption)
        {
            var filteredData = sourceData;

            if (searchKeyword != null)
            {
                filteredData = SearchProperty(filteredData, searchKeyword);
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                filteredData = SortByProperty(filteredData, sortColumn, sortOption);
            }

            var totalItems = filteredData.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PaginationResult<T>
            {
                Items = filteredData.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        private static IQueryable<T> SearchProperty(IQueryable<T> data, dynamic searchKeyword)
        {
            List<T> searchedData = new List<T>();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                List<T> value = data.AsEnumerable().Where(item => prop.GetValue(item) != null && prop.GetValue(item).ToString().Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var item in value)
                {
                    searchedData.Add(item);
                }
            }

            return searchedData.AsQueryable();
        }

        private static IQueryable<T> SortByProperty(IQueryable<T> sourceData, string sortColumn, string sortOption)
        {
            var type = typeof(T);
            ParameterExpression param = Expression.Parameter(type, "p");
            MemberExpression property = Expression.Property(param, sortColumn);
            LambdaExpression lambda = Expression.Lambda(property, param);
            string methodName = sortOption.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "OrderBy" : "OrderByDescending";
            MethodCallExpression result = Expression.Call(typeof(Queryable), methodName, new Type[] { type, property.Type }, sourceData.Expression, lambda);
            return sourceData.Provider.CreateQuery<T>(result);
        }
    }
}