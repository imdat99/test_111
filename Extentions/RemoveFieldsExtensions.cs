using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System;
using System.Linq;

namespace Acm.Api.Extensions
{
    public class RemoveFieldsInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectType)
            {
                // Xóa fields có tên Clone
                var fieldsToRemove = objectType.Fields
                    .Where(f => f.Name.Equals("Clone", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Equals("CalculateSize", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Equals("ToDateTimeOffset", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Equals("ToDiagnosticString", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                foreach (var field in fieldsToRemove)
                {
                    objectType.Fields.Remove(field);
                }
            }
        }
    }
}
