using DevExpress.ClipboardSource.SpreadsheetML;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class SetMachineTagModel
    {
        [Required]
        public string MachineName { get; set; } = null!;
        [Required]
        public string TagName { get; set; } = null!;
        [Required]
        public int DataType { get; set; }

        [ValueStringValidationAttribute(nameof(DataType))]
        public string ValueString { get; set; } = null!;
    }

    public class ValueStringValidationAttribute : ValidationAttribute
    {
        private readonly string datatypePropName;

        public ValueStringValidationAttribute(string datatypePropertyName)
        {
            datatypePropName = datatypePropertyName;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            string? valString = value as string;

            if (string.IsNullOrEmpty(valString))
            {
                return new ValidationResult("ValueString cannot be null or empty.");
            }


            var datatypeProperty = validationContext.ObjectType.GetProperty(datatypePropName);
            
            if (datatypeProperty is null )
            {
                return new ValidationResult($"Property {datatypePropName} not found");
            }
            var datatypeValue = datatypeProperty.GetValue(validationContext.ObjectInstance);

            if (datatypeValue is null || !(datatypeValue is int datatype) )
            {
                return new ValidationResult("Datatype and AmountValue must be a valid integer.");
            }

            bool verifyRes = MachineTypeEnumHelper.VerifyValueStringWithDatatype((int)datatypeValue, valString);
            return verifyRes ? ValidationResult.Success : new ValidationResult("value string format error", new[] { "ValueString" });
        }

    }
}
