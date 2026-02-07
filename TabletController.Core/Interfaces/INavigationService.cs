namespace TabletController.Core.Interfaces
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);
        Task NavigateToAsync(string route, IDictionary<string, object> parameters);
        Task NavigateToAsync(string route, IDictionary<string, object> parameters, bool skipHistoryPush);
        Task GoBackAsync();
        void PushHistoryEntry(string route, IDictionary<string, object>? parameters = null);
    }
}
