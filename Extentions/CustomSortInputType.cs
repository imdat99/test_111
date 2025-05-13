using HotChocolate.Data.Sorting;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Acm.Api.CustomTypes
{
    /// <summary>
    /// Loại bỏ các thuộc tính dạng liên kết khỏi SORTING (không cho phép lọc theo thông tin bảng khác)
    /// </summary>
    public class CustomSortInputType<T> : SortInputType<T>
    {
        protected override void Configure(ISortInputTypeDescriptor<T> descriptor)
        {
            base.Configure(descriptor);
            var properties = typeof(T).GetProperties().Where(p => !p.PropertyType.IsValueType && p.PropertyType != typeof(string)).ToList();
            properties.ForEach(p => descriptor.Ignore(GenerateGetterLambda(p)));
        }

        private static Expression<Func<T, object>> GenerateGetterLambda(PropertyInfo property)
        {
            var objParameterExpr = Expression.Parameter(typeof(T), "x");
            var propertyExpr = Expression.Property(objParameterExpr, property);
            return Expression.Lambda<Func<T, object>>(propertyExpr, objParameterExpr);
        }
    }
}
