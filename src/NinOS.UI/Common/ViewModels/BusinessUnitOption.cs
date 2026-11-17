using NinOS.Domain;

namespace NinOS.UI.Common.ViewModels
{
    public sealed class BusinessUnitOption
    {
        public BusinessUnitOption(business_unit unit)
        {
            this.unit = unit;
        }

        public business_unit unit { get; }

        public string label => BusinessUnitRules.label_of(unit);
    }
}
