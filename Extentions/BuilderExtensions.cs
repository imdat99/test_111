using Acm.Api.CustomTypes;
using Acm.Api.Helpers;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Acm.Api.Extensions
{
    public static class BuilderExtensions
    {
        /// <summary>
        /// Đăng ký một loạt các đối tượng TYPE dạng custom
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IRequestExecutorBuilder AddCustomTypes(this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            //lấy toàn bộ type không phải của hotchocolate
            var allTypes = TypeHelpers.GetAllTypes();
            Type dependentType;
            List<Type> inputTypes;

            //-----lấy ra các type (distinct) để đăng ký cùng kiểu
            //đăng ký các đối tượng kế thừa ObjectType<T>
            dependentType = typeof(ObjectType);
            inputTypes = allTypes.Where(dependentType.IsAssignableFrom).ToList();

            //đăng ký các đối tượng kế thừa FilterInputType<T>
            dependentType = typeof(FilterInputType);
            inputTypes.AddRange(allTypes.Where(p => dependentType.IsAssignableFrom(p) && p.BaseType.Name == typeof(CustomFilterInputType<object>).Name).ToList());

            //đăng ký các đối tượng kế thừa SortInputType<T>
            dependentType = typeof(SortInputType);
            inputTypes.AddRange(allTypes.Where(p => dependentType.IsAssignableFrom(p) && p.BaseType.Name == typeof(CustomSortInputType<object>).Name).ToList());
            inputTypes = inputTypes.Distinct().ToList();
            inputTypes.ForEach(type => builder.AddType(type));
            //---

            //Đăng ký các đối tượng sử dụng thuộc tính ExtendObjectTypeAttribute
            dependentType = typeof(ExtendObjectTypeAttribute);
            inputTypes = allTypes.Where(p => p.IsDefined(dependentType)).ToList();
            inputTypes.ForEach(type => builder.AddTypeExtension(type));

            //Đăng ký các đối tượng custom ScalarType
            dependentType = typeof(ScalarType);
            inputTypes = allTypes.Where(p => dependentType.IsAssignableFrom(p)).ToList();
            inputTypes.ForEach(type => builder.AddTypeExtension(type));

            //thêm các kiểu dữ liệu Unsigned
            builder.AddType<UnsignedIntType>()
            .AddType<UnsignedLongType>()
            .AddType<UnsignedShortType>()
            .AddType<UploadType>()
            .AddType<AnyType>();
            return builder;
        }
    }
}
