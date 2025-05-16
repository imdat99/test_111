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
        private readonly BlockConfig _blockConfig;
        public IgnoreFieldsMiddleware(FieldDelegate next, BlockConfig blockConfig)
        {
            _next = next;
            _blockConfig = blockConfig;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var document = context.Operation!.Document;
            var _syntaxWalker = new MySyntaxWalker(_blockConfig);
            var syntax = _syntaxWalker.Visit(document, null);
            var violations = _syntaxWalker.Violations.Distinct().ToList();
            var path = String.Join(".", context.Path.ToList().OfType<string>());
            if (violations.Count != 0)
            {
                violations.ForEach(violation =>
                {
                    var blockedField = violation;
                    context.ReportError(ErrorBuilder.New()
                        .SetMessage($"Field '{blockedField}' bị chặn bởi chính sách bảo mật.")
                        .SetCode("BLOCKED_FIELD")
                        .SetPath(context.Path)
                        .Build());
                    context.Result = null;
                    return;
                });
            }
            await _next(context);
        }
    }
    public class MySyntaxWalker : SyntaxWalker<object?>
    {
        private readonly List<BlockRule> _rules;
        private readonly List<string> _violations = new();
        public IReadOnlyList<string> Violations => _violations;

        public MySyntaxWalker(BlockConfig blockConfig)
        {
            _rules = blockConfig.BlockRules;
        }

        protected override ISyntaxVisitorAction Enter(OperationDefinitionNode node, object? context)
        {
            foreach (var rootField in node.SelectionSet.Selections.OfType<FieldNode>())
            {
                TraverseField(rootField, rootField.Name.Value, rootField.Name.Value);
            }

            return Continue;
        }

        private void TraverseField(FieldNode field, string currentPath, string rootQuery)
        {
            _violations.AddRange(_rules.Where(r => r.Query == rootQuery).SelectMany(r => r.Fields.Select(field => string.Join(".", [r.Query, field]))).Where(x => x == currentPath));
            if (field.SelectionSet != null)
            {
                foreach (var child in field.SelectionSet.Selections.OfType<FieldNode>())
                {
                    var childPath = $"{currentPath}.{child.Name.Value}";
                    TraverseField(child, childPath, rootQuery);
                }
            }
        }

        protected override ISyntaxVisitorAction Leave(OperationDefinitionNode node, object? context)
        {
            return Continue;
        }
    }

}