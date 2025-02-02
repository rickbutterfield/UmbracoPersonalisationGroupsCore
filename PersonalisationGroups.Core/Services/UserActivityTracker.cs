using Microsoft.Extensions.Options;
using Our.Umbraco.PersonalisationGroups.Core.Configuration;
using Our.Umbraco.PersonalisationGroups.Core.Criteria.PagesViewed;
using Our.Umbraco.PersonalisationGroups.Core.Providers.Cookie;
using Our.Umbraco.PersonalisationGroups.Core.Providers.DateTime;
using Our.Umbraco.PersonalisationGroups.Core.Providers.PagesViewed;

namespace Our.Umbraco.PersonalisationGroups.Core.Services
{
    public class UserActivityTracker : IUserActivityTracker
    {
        private readonly PersonalisationGroupsConfig _config;
        private readonly ICookieProvider _cookieProvider;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UserActivityTracker(IOptions<PersonalisationGroupsConfig> config, ICookieProvider cookieProvider, IDateTimeProvider dateTimeProvider)
        {
            _config = config.Value;
            _cookieProvider = cookieProvider;
            _dateTimeProvider = dateTimeProvider;
        }

        public void TrackPageView(int pageId)
        {
            var key = _config.CookieKeyForTrackingPagesViewed;
            var value = _cookieProvider.GetCookieValue(key);
            if (!string.IsNullOrEmpty(value))
            {
                value = AppendPageIdIfNotPreviouslyViewed(value, pageId);
            }
            else
            {
                value = pageId.ToString();
            }

            var expires = _dateTimeProvider.GetCurrentDateTime().AddDays(_config.ViewedPagesTrackingCookieExpiryInDays);
            _cookieProvider.SetCookie(key, value, expires);
        }

        internal static string AppendPageIdIfNotPreviouslyViewed(string viewedPageIds, int pageId)
        {
            var ids = viewedPageIds.ParsePageIds();

            if (!ids.Contains(pageId))
            {
                ids.Add(pageId);
            }

            return string.Join(",", ids);
        }

        public void TrackSession()
        {
            var key = _config.CookieKeyForTrackingIfSessionAlreadyTracked;

            // Check if session cookie present.
            var sessionCookieExists = _cookieProvider.CookieExists(key);
            if (!sessionCookieExists)
            {
                // If not, create or update the number of visits cookie.
                var value = _cookieProvider.GetCookieValue(_config.CookieKeyForTrackingNumberOfVisits);
                if (!string.IsNullOrEmpty(value))
                {
                    value = int.TryParse(value, out int cookieValue) ? (cookieValue + 1).ToString() : "1";
                }
                else
                {
                    value = "1";
                }

                var expires = _dateTimeProvider.GetCurrentDateTime().AddDays(_config.NumberOfVisitsTrackingCookieExpiryInDays);
                _cookieProvider.SetCookie(key, value, expires);

                // Set the session cookie so we don't keep updating on each request
                _cookieProvider.SetCookie(_config.CookieKeyForTrackingIfSessionAlreadyTracked, "1");
            }
        }
    }
}