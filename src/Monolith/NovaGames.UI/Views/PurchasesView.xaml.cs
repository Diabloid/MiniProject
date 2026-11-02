using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovaGames.Core.Interfaces;
using NovaGames.UI.Services.Api;

namespace NovaGames.UI.Views;

public partial class PurchasesView : UserControl
{
    private readonly IGatewayApiClient _apiClient;
    private readonly ILogger? _logger;
    public ObservableCollection<ClientOrderDto> Orders { get; set; } = new();

    public PurchasesView()
    {
        InitializeComponent();

        _apiClient = App.AppHost!.Services.GetRequiredService<IGatewayApiClient>();
        _logger = App.AppHost!.Services.GetService<ILogger>();

        this.Loaded += async (s, e) => await LoadOrdersAsync();
    }

    public async Task LoadOrdersAsync()
    {
        SetState(ViewState.Loading);

        try
        {
            var pagedResult = await _apiClient.GetMyOrdersAsync(page: 1, pageSize: 50);

            Orders.Clear();
            if (pagedResult.Items.Count == 0)
            {
                SetState(ViewState.Empty);
            }
            else
            {
                foreach (var order in pagedResult.Items)
                {
                    Orders.Add(order);
                }
                listOrders.ItemsSource = Orders;
                SetState(ViewState.Success);
            }
        }
        catch (RateLimitExceededException ex)
        {
            txtErrorMessage.Text = $"Забагато запитів. Зачекайте {ex.RetryAfterSeconds} с перед повтором.";
            SetState(ViewState.Error);
        }
        catch (ApiException ex)
        {
            txtErrorMessage.Text = ex.Message;
            SetState(ViewState.Error);
        }
        catch (Exception ex)
        {
            _ = _logger?.LogAsync($"Помилка завантаження замовлень: {ex.Message}", NovaGames.Core.Models.LogType.ERROR);
            txtErrorMessage.Text = "Не вдалося з'єднатися з API Gateway (порт 5000).";
            SetState(ViewState.Error);
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }

    private async void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }

    private void BtnGoToCatalog_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this) as MainWindow;
        mainWindow?.NavigateToStore();
    }

    private async void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int orderId) return;

        var confirm = MessageBox.Show($"Ви дійсно бажаєте скасувати замовлення #{orderId}?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        btn.IsEnabled = false;
        btn.Content = "СКАСУВАННЯ...";

        try
        {
            var (success, error) = await _apiClient.CancelOrderAsync(orderId);
            if (success)
            {
                MessageBox.Show($"Замовлення #{orderId} успішно скасовано.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                MessageBox.Show(error ?? "Не вдалося скасувати замовлення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                btn.IsEnabled = true;
                btn.Content = "СКАСУВАТИ";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Помилка зв'язку зі шлюзом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            btn.IsEnabled = true;
            btn.Content = "СКАСУВАТИ";
        }
    }

    private enum ViewState { Loading, Success, Empty, Error }

    private void SetState(ViewState state)
    {
        panelLoading.Visibility = state == ViewState.Loading ? Visibility.Visible : Visibility.Collapsed;
        panelError.Visibility = state == ViewState.Error ? Visibility.Visible : Visibility.Collapsed;
        panelEmpty.Visibility = state == ViewState.Empty ? Visibility.Visible : Visibility.Collapsed;
        scrollContent.Visibility = state == ViewState.Success ? Visibility.Visible : Visibility.Collapsed;
    }
}
