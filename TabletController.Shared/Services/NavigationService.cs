using TabletController.Core.Interfaces;

namespace TabletController.Shared.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Stack<(string Route, IDictionary<string, object>? Parameters)> _navigationHistory = new();
        private IDictionary<string, object>? _currentPageParameters;

        public async Task NavigateToAsync(string route)
        {
            // Track current location before navigating
            var currentRoute = Shell.Current.CurrentState.Location.ToString();
            if (!string.IsNullOrEmpty(currentRoute))
            {
                var routeWithoutQuery = currentRoute.Split('?')[0];
                _navigationHistory.Push((routeWithoutQuery, _currentPageParameters));
            }

            _currentPageParameters = null;
            await Shell.Current.GoToAsync(route);
        }

        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            await NavigateToAsync(route, parameters, skipHistoryPush: false);
        }

        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters, bool skipHistoryPush)
        {
            // Track current location before navigating (with current page's parameters)
            if (!skipHistoryPush)
            {
                var currentRoute = Shell.Current.CurrentState.Location.ToString();
                if (!string.IsNullOrEmpty(currentRoute))
                {
                    var routeWithoutQuery = currentRoute.Split('?')[0];
                    _navigationHistory.Push((routeWithoutQuery, _currentPageParameters));
                }
            }

            // Store the parameters for the page we're navigating to
            _currentPageParameters = new Dictionary<string, object>(parameters);

            var navigationParameters = new ShellNavigationQueryParameters();
            foreach (var parameter in parameters)
            {
                navigationParameters.Add(parameter.Key, parameter.Value);
            }
            await Shell.Current.GoToAsync(route, navigationParameters);
        }

        public async Task GoBackAsync()
        {
            if (_navigationHistory.Count > 0)
            {
                var (previousRoute, parameters) = _navigationHistory.Pop();

                // Restore the parameters for the page we're going back to
                _currentPageParameters = parameters;

                if (parameters != null && parameters.Count > 0)
                {
                    var navigationParameters = new ShellNavigationQueryParameters();
                    foreach (var parameter in parameters)
                    {
                        navigationParameters.Add(parameter.Key, parameter.Value);
                    }
                    await Shell.Current.GoToAsync(previousRoute, navigationParameters);
                }
                else
                {
                    await Shell.Current.GoToAsync(previousRoute);
                }
            }
        }

        public void PushHistoryEntry(string route, IDictionary<string, object>? parameters = null)
        {
            _navigationHistory.Push((route, parameters != null ? new Dictionary<string, object>(parameters) : null));
        }
    }
}
