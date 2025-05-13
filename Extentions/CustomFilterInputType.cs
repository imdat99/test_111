using HotChocolate.Data.Filters;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Acm.Api.CustomTypes
{
    /// <summary>
    /// Loại bỏ các thuộc tính dạng liên kết khỏi IFILTERING (không cho phép lọc theo thông tin bảng khác)
    /// </summary>
    public class CustomFilterInputType<T> : FilterInputType<T>
    {
        protected override void Configure(IFilterInputTypeDescriptor<T> descriptor)
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

    public class CustomComparableOperationFilterInputType<T> : ComparableOperationFilterInputType<T>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            base.Configure(descriptor);

            string customName;
            var type = typeof(T);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                customName = Nullable.GetUnderlyingType(type)!.Name + nameof(Nullable);
            }
            else
                customName = type.Name;

            descriptor.Name(customName);
        }
    }
}
