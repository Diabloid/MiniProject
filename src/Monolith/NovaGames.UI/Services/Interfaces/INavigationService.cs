using System.Windows;

public interface INavigationService
{
    void NavigateTo<TDestination>(Window currentWindow) where TDestination : Window;
}