using Acm.Api.Helpers;
using Acm.Api.Models;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Acm.Api.Middlewares
{
    public class FieldMiddleware
    {
        private readonly FieldDelegate _next;

        public FieldMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var syntaxNode = context.Selection.SyntaxNode;
            var arguments = syntaxNode.Arguments;
            if (arguments.Count == 0)
            {
                await _next(context);
                return;
            }

            //get validation errors
            var validationErrors = new List<ValidationFailure>();
            var argumentValues = arguments.Select(x => context.ArgumentValue<object>(x.Name.Value)).ToList();
            argumentValues.ForEach(arg =>
            {
                var resultValidator = Validate(arg);
                if (resultValidator != null)
                    validationErrors.AddRange(resultValidator.Errors);
            });

            if (validationErrors.Count > 0)
            {
                ShowError(context, validationErrors, syntaxNode);
                return;
            }

            await _next(context);
        }

        static void ShowError(IMiddlewareContext context, IEnumerable<ValidationFailure> validationErrors, HotChocolate.Language.FieldNode syntaxNode)
        {
            //show GRAPHQL ERROR
            foreach (var error in validationErrors)
            {
                var ier = Utilities.BuildError(error.ErrorMessage,
                    error.ErrorCode,
                    null,
                    context.Path,
                    new Dictionary<string, object>{
                        { "memberNames", error.PropertyName }
                    },
                    syntaxNode.Location != null ?
                new List<HotChocolate.Location> {
                    new HotChocolate.Location(syntaxNode.Location.Line, syntaxNode.Location.Column)
                } : null
                    );
                context.ReportError(ier);
            }
            context.Result = null;
        }

        static ValidationResult Validate(object obj)
        {
            try
            {
                if (obj == null) return null;
                var validatorType = GetValidatorType(obj.GetType());
                if (validatorType == null) return null;
                var validator = Activator.CreateInstance(validatorType);
                var method = validatorType.GetMethods().FirstOrDefault(x => x.Name == nameof(IValidator<object>.Validate));
                return (ValidationResult)method?.Invoke(validator, new object[] { obj });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        static Type? GetValidatorType(Type type)
        {
            var typeOfAbstractValidator = typeof(AbstractValidator<>).MakeGenericType(type);
            return TypeHelpers.GetAllTypes().Where(x => typeOfAbstractValidator.IsAssignableFrom(x)).FirstOrDefault();
        }
    }

    // Attribute để đánh dấu các field cần bị chặn khi truy cập
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class IgnoreFieldsAttribute : Attribute
    {
        public string[] FieldNames { get; }
        public bool BlockFieldAccess { get; }

        /// <summary>
        /// Xác định có áp dụng việc chặn cho các field con bên trong field cha hay không
        /// </summary>
        public bool ApplyToNestedFields { get; }

        /// <summary>
        /// Chặn các field được chỉ định và các field con bên trong chúng (nếu có)
        /// </summary>
        /// <param name="fieldNames">Danh sách tên field cần chặn</param>
        public IgnoreFieldsAttribute(params string[] fieldNames)
            : this(true, true, fieldNames) { }

        /// <summary>
        /// Chặn hoặc ẩn các field được chỉ định và có thể áp dụng cho các field con
        /// </summary>
        /// <param name="blockFieldAccess">Nếu true, chặn hoàn toàn field. Nếu false, chỉ đặt giá trị về mặc định</param>
        /// <param name="fieldNames">Danh sách tên field cần chặn</param>
        public IgnoreFieldsAttribute(bool blockFieldAccess, params string[] fieldNames)
            : this(blockFieldAccess, true, fieldNames) { }

        /// <summary>
        /// Chặn hoặc ẩn các field được chỉ định và kiểm soát việc áp dụng cho các field con
        /// </summary>
        /// <param name="blockFieldAccess">Nếu true, chặn hoàn toàn field. Nếu false, chỉ đặt giá trị về mặc định</param>
        /// <param name="applyToNestedFields">Nếu true, áp dụng việc chặn cho cả các field con bên trong field cha</param>
        /// <param name="fieldNames">Danh sách tên field cần chặn</param>
        public IgnoreFieldsAttribute(bool blockFieldAccess, bool applyToNestedFields, params string[] fieldNames)
        {
            FieldNames = fieldNames;
            BlockFieldAccess = blockFieldAccess;
            ApplyToNestedFields = applyToNestedFields;
        }
    }

    public class IgnoreFieldsMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly MySyntaxWalker _syntaxWalker;
        private readonly BlockConfig _blockConfig;
        public IgnoreFieldsMiddleware(FieldDelegate next, BlockConfig blockConfig)
        {
            _next = next;
            _syntaxWalker = new MySyntaxWalker(blockConfig);
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            // Kiểm tra field hiện tại có bị chặn không
            var currentField = context.Selection.Field;
            var currentFieldName = currentField.Name;
            var objectType = context.ObjectType.Name;             // "User"
            var operationName = context.Operation?.Name;  // "userPaging" hoặc "userDetail"
            var document = context.Operation!.Document;
            // document.Visit(context.Operation);
            var syntax = _syntaxWalker.Visit(document, null);
            var pathSegments = context.Path.ToList();
            if (pathSegments.Contains("todos"))
            {
                context.ReportError(ErrorBuilder.New()
                                .SetMessage($"Field '{currentFieldName}' bị chặn bởi chính sách bảo mật.")
                                .SetCode("BLOCKED_FIELD")
                                .SetPath(context.Path)
                                .Build());
                context.Result = null;
                return;
            }
            var selections = context.Selection.SyntaxNode?.SelectionSet?.Selections;

            // 1. Lấy ra các IgnoreFieldsAttribute từ mọi resolver
            var resolvers = currentField.DeclaringType?.Fields
                .Where(f => f.Member != null)
                .Select(f => f.Member);
            var ignoreAttr = currentField.Member?.GetCustomAttribute<IgnoreFieldsAttribute>();
            if (resolvers != null)
            {
                foreach (var resolver in resolvers)
                {
                    var ignoreAttrs = new List<IgnoreFieldsAttribute>();
                    ignoreAttrs.AddRange(resolver?.GetCustomAttributes<IgnoreFieldsAttribute>() ?? []);
                    if (ignoreAttr != null) ignoreAttrs.Add(ignoreAttr);

                    foreach (var attr in ignoreAttrs)
                    {
                        if (attr.FieldNames.Contains(currentFieldName, StringComparer.OrdinalIgnoreCase))
                        {
                            // Chặn field và trả về lỗi
                            context.ReportError(ErrorBuilder.New()
                                .SetMessage($"Field '{currentFieldName}' bị chặn bởi chính sách bảo mật.")
                                .SetCode("BLOCKED_FIELD")
                                .SetPath(context.Path)
                                .Build());
                            context.Result = null;
                            return;
                        }
                    }
                }
            }

            // 2. Tiếp tục xử lý nếu field không bị chặn
            await _next(context);

            // 3. Sau khi xử lý field, không còn xử lý phần đặt giá trị mặc định nữa
            // vì yêu cầu là chặn hoàn toàn việc xử lý field
        }
    }
    public class MySyntaxWalker : SyntaxWalker<object?>
    {
        // protected override ISyntaxVisitorAction OnBeforeEnter(OperationDefinitionNode node, object? context)
        // {
        //     // Thực hiện hành động trước khi vào OperationDefinitionNode
        //     Console.WriteLine($"Đang chuẩn bị vào Operation: {node.Name?.Value ?? "<unnamed>"}");

        //     // Trả về hành động tiếp tục duyệt cây cú pháp
        //     return Continue;
        // }
        private readonly List<BlockRule> _rules;
        private readonly List<(string ParentField, string BlockedField)> _violations = new();
        private readonly Stack<string> _fieldPath = new();

        public IReadOnlyList<(string ParentField, string BlockedField)> Violations => _violations;
        public MySyntaxWalker(BlockConfig blockConfig)
        {
            _rules = blockConfig.BlockRules;
        }
        protected override ISyntaxVisitorAction Enter(OperationDefinitionNode node, object? context)
        {
            // Xử lý khi vào OperationDefinitionNode
            _fieldPath.Push(node.Name!.Value);

            if (_fieldPath.Count >= 2)
            {
                var parent = _fieldPath.Skip(1).First(); // immediate parent
                var current = _fieldPath.Peek();

                var matchedRule = _rules.FirstOrDefault(r => r.UnderField == parent);
                if (matchedRule != null && matchedRule.Fields.Contains(current))
                {
                    _violations.Add((parent, current));
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(OperationDefinitionNode node, object? context)
        {
            _fieldPath.Pop();
            return Continue;
        }
    }

}