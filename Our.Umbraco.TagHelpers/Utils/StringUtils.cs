using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Utils
{
    internal static class StringUtils
    {
        internal static double GetDouble(object value, double defaultValue = 0)
        {
            double num;
            if (!IsDouble(value))
            {
                return defaultValue;
            }
            try
            {
                num = Convert.ToDouble(value);
            }
            catch
            {
                num = defaultValue;
            }
            return num;
        }

        internal static decimal GetDecimal(object input, decimal defaultValue = 0)
        {
            decimal value = decimal.MinValue;
            decimal.TryParse(input != null ? input.ToString().Replace("£", "") : "", out value);

            if (value > decimal.MinValue)
            {
                return value;
            }

            return defaultValue;
        }

        internal static int GetInteger(object input, int defaultValue = 0)
        {
            var value = 0;
            int.TryParse(input != null ? input.ToString() : "", out value);

            if (value > 0)
            {
                return value;
            }

            return defaultValue;
        }

        internal static bool IsDouble(object value)
        {
            double num;
            if (IsNull(value))
            {
                return false;
            }
            if (value is double)
            {
                return true;
            }
            string str = Convert.ToString(value);
            if (double.TryParse(str, out num))
            {
                return true;
            }
            return false;
        }

        internal static bool IsNull(object value)
        {
            if (value == null)
            {
                return true;
            }
            return value == DBNull.Value;
        }
    }
}
