using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GoogleAnalyticsTracker.Core.TrackerParameters;
using GoogleAnalyticsTracker.Core.TrackerParameters.Interface;

namespace GoogleAnalyticsTracker.Core
{
    public partial class TrackerBase
    {
        private static IDictionary<string, string> GetParametersDictionary(IGeneralParameters parameters)
        {
            var beaconList = new BeaconList<string, string>();

            foreach (var p in parameters.GetType().GetRuntimeProperties())
            {
                var attr = p.GetCustomAttribute(typeof(BeaconAttribute), true) as BeaconAttribute;

                if (attr == null)
                {
                    continue;
                }

                object value;

                if ((p.PropertyType.GetTypeInfo().IsEnum || p.PropertyType.IsNullableEnum()) && attr.IsEnumByValueBased)
                {
                    value = GetValueFromEnum(p, parameters) ?? p.GetMethod.Invoke(parameters, null);
                }
                else if (p.PropertyType.GetTypeInfo().IsEnum || p.PropertyType.IsNullableEnum())
                {
                    value = GetLowerCaseValueFromEnum(p, parameters) ?? p.GetMethod.Invoke(parameters, null);
                }
                else
                {
                    value = p.GetMethod.Invoke(parameters, null);
                }

                if (value != null)
                {
                    beaconList.Add(attr.Name, value.ToString());
                }
            }

            beaconList.ShiftToLast("dp");
            beaconList.ShiftToLast("z");

            return beaconList
                .OrderBy(k => k.Item1, new BeaconComparer())
                .ToDictionary(key => key.Item1, value => value.Item2);
        }

        private static List<IDictionary<string, string>> GetParametersDictionaryList(IEnumerable<IGeneralParameters> parameters)
        {
            return parameters.Select(p => GetParametersDictionary(p)).ToList();
        }

        private static object GetValueFromEnum(PropertyInfo propertyInfo, IGeneralParameters parameters)
        {
            var value = propertyInfo.GetMethod.Invoke(parameters, null);

            if (value == null) return null;

            var enumValue =
                Enum.Parse(propertyInfo.PropertyType.IsNullableEnum()
                        ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                        : propertyInfo.PropertyType, value.ToString());

            return enumValue.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        private static object GetLowerCaseValueFromEnum(PropertyInfo propertyInfo, IGeneralParameters parameters)
        {
            var value = propertyInfo.GetMethod.Invoke(parameters, null);

            return value == null 
                ? null 
                : value.ToString().ToLowerInvariant();
        }

        private void SetRequired(IGeneralParameters parameters)
        {
            parameters.TrackingId = TrackingAccount;
            parameters.DocumentHostName = TrackingDomain;

            if (string.IsNullOrWhiteSpace(parameters.ClientId))
            {
                parameters.ClientId = AnalyticsSession.GenerateSessionId();
            }

            if (string.IsNullOrWhiteSpace(parameters.UserLanguage))
            {
                // Note: another way could be CultureInfo.CurrentCulture
                parameters.UserLanguage = "en-US";
            }            
        }        

        public async Task<TrackingResult> TrackAsync(IGeneralParameters generalParameters)
        {
            SetRequired(generalParameters);

            var parameters = GetParametersDictionary(generalParameters);

            var url = UseSsl
                ? BeaconUrlSsl
                : BeaconUrl;

            return await RequestUrlAsync(url, parameters, generalParameters.UserAgent ?? UserAgent);
        }

        public async Task<TrackingResult> TrackAsync(IEnumerable<IGeneralParameters> generalParameters)
        {
            generalParameters = generalParameters.Select(p => { SetRequired(p); return p; });

            var parameters = GetParametersDictionaryList(generalParameters);

            var url = UseSsl
                ? BatchUrlSsl
                : BatchUrl;

            return await PostUrlAsync(url, parameters, generalParameters.First().UserAgent ?? UserAgent);
        }
    }
}